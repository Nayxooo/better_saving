using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using better_saving.Models; // For JobType, JobStates, Hashing, Backup, Settings
using better_saving.ViewModels; // Required for MainViewModel reference

// Job class to store job information
public class backupJob : INotifyPropertyChanged
{
    // CancellationTokenSource for job execution - persists across ViewModel instances
    public CancellationTokenSource? _executionCts;

    private string _name = string.Empty; // Initialize to default
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    private string _sourceDirectory = string.Empty; // Initialize to default
    public string SourceDirectory
    {
        get => _sourceDirectory;
        private set
        {
            if (_sourceDirectory != value)
            {
                _sourceDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    private string _targetDirectory = string.Empty; // Initialize to default
    public string TargetDirectory
    {
        get => _targetDirectory;
        private set
        {
            if (_targetDirectory != value)
            {
                _targetDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    private JobType _type;
    public JobType Type
    {
        get => _type;
        private set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged();
            }
        }
    }
    private JobStates _state;
    private readonly bool _initializing = true; // Flag to prevent state.json updates during initialization
    public JobStates State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();

                // Set Progress to 0 when state is Failed
                if (value == JobStates.Failed)
                {
                    Progress = 0;
                }

                if (value == JobStates.Finished || value == JobStates.Stopped || value == JobStates.Paused)
                {
                    // Reset IsPausing when job is finished or stopped (security measure)
                    IsPausing = false;
                }

                // Only update state.json if not initializing and logger is available
                if (!_initializing && BackupJobLogger != null)
                {
                    try
                    {
                        BackupJobLogger.UpdateAllJobsState();
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception, but don't let it disrupt the app
                        Console.Error.WriteLine($"Error updating job state: {ex.Message}");
                    }
                }
            }
        }
    }

    private bool _isPausing;
    public bool IsPausing
    {
        get => _isPausing;
        set
        {
            if (_isPausing != value)
            {
                _isPausing = value;
                OnPropertyChanged();
            }
        }
    }

    private int _totalFilesToCopy;
    public int TotalFilesToCopy
    {
        get => _totalFilesToCopy;
        private set
        {
            if (_totalFilesToCopy != value)
            {
                _totalFilesToCopy = value;
                OnPropertyChanged();
            }
        }
    }

    private long _totalFilesCopied;
    public long TotalFilesCopied
    {
        get => _totalFilesCopied;
        internal set // Can be set from within the assembly, e.g. by ExecuteAsync
        {
            if (_totalFilesCopied != value)
            {
                _totalFilesCopied = value;
                OnPropertyChanged();
            }
        }
    }

    private ulong _totalSizeToCopy;
    public ulong TotalSizeToCopy
    {
        get => _totalSizeToCopy;
        private set
        {
            if (_totalSizeToCopy != value)
            {
                _totalSizeToCopy = value;
                OnPropertyChanged();
            }
        }
    }

    private long _totalSizeCopied;
    public long TotalSizeCopied
    {
        get => _totalSizeCopied;
        internal set // Can be set from within the assembly
        {
            if (_totalSizeCopied != value)
            {
                _totalSizeCopied = value;
                OnPropertyChanged();
            }
        }
    }

    private int _numberFilesLeftToDo;
    public int NumberFilesLeftToDo
    {
        get => _numberFilesLeftToDo;
        private set
        {
            if (_numberFilesLeftToDo != value)
            {
                _numberFilesLeftToDo = value;
                OnPropertyChanged();
            }
        }
    }

    private readonly List<string> FilesToBackup = [];

    public int IdleTime = 300000; // 5 minutes default idle time

    private float _progress;
    public float Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private readonly string CryptoSoftExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");
    private readonly string CryptoSoftSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.settings");

    private readonly Logger? BackupJobLogger;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Ajout des champs manquants
    private readonly Func<bool> _isSoftwareRunning;
    private readonly Action<backupJob> _addToBlockedJobs;
    private readonly MainViewModel? _mainViewModel; // Add this field

    /// <summary>
    /// Encrypts a file if its extension is in the list of extensions to encrypt.
    /// </summary>
    /// <param name="filePath">The path to the file to encrypt.</param>
    /// <returns>The exit code from the encryption process (0 if no encryption was needed).</returns>
    private int EncryptFileIfNeeded(string filePath)
    {
        var settings = Settings.LoadSettings();
        var extensionsToEncrypt = settings.FileExtensions;

        // Check if the file extension is in the list of extensions to encrypt
        string fileExtension = Path.GetExtension(filePath).ToLower();
        if (!extensionsToEncrypt.Contains(fileExtension))
        {
            return 0; // No encryption needed
        }

        try
        {            // refer to CryptoSoft exit codes to know why the first on is -6 and not -1            
            if (!File.Exists(CryptoSoftExePath))
            {
                // display the popup and try to download CryptoSoft.exe
                System.Windows.MessageBox.Show(
                    "CryptoSoft.exe not found, press OK to download it from the official repository.",
                    "Encryption Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);

                // try to download CryptoSoft.exe (because users can'tbe trused to download it themselves or read the f**king documentation)
                bool downloadSuccess = CryptoSoftDownloader.DownloadCryptoSoft();
                if (!downloadSuccess)
                {
                    return -6; // CryptoSoft.exe download failed
                }

                // Verify the file exists after download
                if (!File.Exists(CryptoSoftExePath))
                {
                    return -7; // CryptoSoft.exe still not found after download attempt
                }
            }
            if (!File.Exists(CryptoSoftSettingsPath))
            {
                // if it doesn't exit, create a default settings file
                // create a random encryption key
                string key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)); // 32 bytes = 256 bits
                var defaultSettings = new { EncryptionKey = key };
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(defaultSettings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CryptoSoftSettingsPath, jsonContent);
            }

            ProcessStartInfo psi = new()
            {
                FileName = CryptoSoftExePath,
                Arguments = $"\"{filePath}\" \"{CryptoSoftSettingsPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process? proc = Process.Start(psi);
            if (proc == null) return -8; // Failed to start process

            proc.WaitForExit();
            return proc.ExitCode;
        }
        catch (Exception)
        {
            return -9; // Exception occurred during encryption
        }
    }
    // Ajout des paramètres manquants au constructeur
    public backupJob(string name, string sourceDir, string targetDir, JobType type, Logger? loggerInstance, Func<bool>? isSoftwareRunning = null, Action<backupJob>? addToBlockedJobs = null, MainViewModel? mainViewModel = null)
    {
        // Set _initializing to true at the very beginning to prevent UpdateAllJobsState calls
        _initializing = true;

        // Initialize properties
        Name = name;
        SourceDirectory = sourceDir;
        TargetDirectory = targetDir;
        Type = type;
        BackupJobLogger = loggerInstance;
        _isSoftwareRunning = isSoftwareRunning ?? (() => false); // Default to no software running if not provided
        _addToBlockedJobs = addToBlockedJobs ?? (_ => { }); // Default to no-op if not provided
        _mainViewModel = mainViewModel; // Store the MainViewModel instance
        _executionCts = new CancellationTokenSource();

        // Initialize default values
        State = JobStates.Stopped;
        IsPausing = false;
        Progress = 0;
        TotalFilesCopied = 0;
        TotalSizeCopied = 0;

        if (!Directory.Exists(sourceDir))
        {
            ErrorMessage = $"Source directory '{sourceDir}' does not exist.";
            State = JobStates.Failed;
            // Initialize counts to reflect failure/no files
            TotalFilesToCopy = 0;
            TotalSizeToCopy = 0;
            NumberFilesLeftToDo = 0;
            // UpdateFilesCountInternalAsync will not be called or will return early if state is Failed.
            // However, to be safe, ensure progress is updated.
            UpdateProgress();
        }
        else
        {
            // Initial scan to set up counts asynchronously
            _ = UpdateFilesCountInternalAsync();
        }

        // After initialization is complete, allow state.json updates
        _initializing = false;
    }

    private async Task UpdateFilesCountInternalAsync()
    {
        // Reset state for the new scan.
        // FilesToBackup is cleared, other counters are reset to accumulate new values.
        FilesToBackup.Clear();
        Progress = 0; // Reset progress to 0 for a new scan
        UpdateProgress(); // Update overall progress and potentially state
        // If this method is called when a job is already in a terminal state (Failed, Stopped),
        // it might not be desired to change it back to Working.
        // However, typically this is called for Stopped jobs or as part of ExecuteAsync.
        // For now, let's assume it's okay to set to Working if not already Failed.
        if (State != JobStates.Failed && State != JobStates.Paused)
        {
            if (_initializing) State = JobStates.Stopped; // Set to Stopped first, then Working in ExecuteAsync
            else State = JobStates.Working; // Indicate scanning is in progress
        }

        TotalSizeToCopy = 0;    // Will accumulate total size of ALL files in source.
        TotalFilesCopied = 0;   // Will accumulate count of files already present/skipped during this scan.
        TotalSizeCopied = 0;    // Will accumulate size of files already present/skipped during this scan.
        NumberFilesLeftToDo = 0;// Will be set to FilesToBackup.Count after the scan.

        string[] sourceFiles;
        try
        {
            sourceFiles = await Task.Run(() => Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error scanning source directory: {ex.Message}";
            State = JobStates.Failed;
            TotalFilesToCopy = 0; // No files to process
            // Other counts (TotalSizeToCopy, TotalFilesCopied, TotalSizeCopied) remain 0 as initialized.
            NumberFilesLeftToDo = 0;
            UpdateProgress(); // Update progress based on failed state
            return;
        }

        TotalFilesToCopy = sourceFiles.Length; // Total number of files found in source

        if (TotalFilesToCopy == 0)
        {
            // No files in source, job is effectively finished or Stoped.
            // UpdateProgress will handle setting state to Finished if appropriate (e.g., if Stopped).
            if (State == JobStates.Working) State = JobStates.Stopped; // If scanning made it working, but no files.
            UpdateProgress();
            return;
        }

        // This Task.Run offloads the file processing (FileInfo, Hashing)
        await Task.Run(() =>
        {
            foreach (var file in sourceFiles)
            {
                // Check for cancellation if this scan is part of a cancellable operation
                // For now, assuming this scan itself isn't directly cancelled mid-way,
                // but relies on the job's overall state (e.g. if ExecuteAsync is cancelled).

                try
                {
                    var fileInfo = new FileInfo(file);
                    ulong currentFileSize = (ulong)fileInfo.Length;

                    // Accumulate total size of all source files
                    TotalSizeToCopy += currentFileSize;

                    var targetFilePath = Path.Combine(TargetDirectory, Path.GetRelativePath(SourceDirectory, file));

                    // seperate if statement to avoid unnecessary hash checks
                    if (!File.Exists(targetFilePath) || Type == JobType.Full)
                    { FilesToBackup.Add(file); }
                    else if (Type == JobType.Diff && !Hashing.CompareFiles(file, targetFilePath))
                    { FilesToBackup.Add(file); }
                    else // File is already backed up or skipped for this scan
                    { TotalFilesCopied++; } // Directly increment member counter for skipped files

                }
                catch (Exception ex)
                {
                    // Log or handle individual file scanning errors
                    Console.WriteLine($"Error processing file {file} during scan: {ex.Message}");
                    // Optionally, count this as an error for the job or skip the file.
                    // If a file error makes the job fail, set State = JobStates.Failed and potentially ErrorMessage.
                }
            } // End foreach
        }); // End Task.Run

        // After processing all files, update NumberFilesLeftToDo based on the populated FilesToBackup list
        NumberFilesLeftToDo = FilesToBackup.Count;
        UpdateProgress(); // Update overall progress and potentially state
    }

    private void UpdateProgress()
    {
        // If the job has failed, the progress should be 0
        if (State == JobStates.Failed)
        {
            Progress = 0;
            return;
        }
        if (TotalFilesToCopy == 0)
        {
            Progress = 100;
        }
        else
        {
            // Ensure TotalFilesCopied doesn't exceed TotalFilesToCopy
            long cappedFilesCopied = Math.Min(TotalFilesCopied, TotalFilesToCopy);
            Progress = (float)(cappedFilesCopied * 100.0 / TotalFilesToCopy);
        }

        if (Progress >= 100 && (State == JobStates.Working || State == JobStates.Stopped) && TotalFilesToCopy > 0)
        {
            // If all files are copied (TotalFilesCopied == TotalFilesToCopy)
            if (TotalFilesCopied == TotalFilesToCopy)
            {
                State = JobStates.Finished;
            }
        }
        else if (TotalFilesToCopy == 0 && State == JobStates.Stopped) // Empty job considered finished
        {
            State = JobStates.Finished;
        }

        // Only update state.json if not initializing to prevent unnecessary writes
        if (!_initializing && BackupJobLogger != null)
        {
            BackupJobLogger.UpdateAllJobsState();
        }
    }


    private bool CheckCancellationRequested(CancellationToken cancellationToken)
    {
        // Check if the cancellation token has been requested
        if (cancellationToken.IsCancellationRequested)
        {
            // If so, set the job state to Stopped and return true
            State = JobStates.Stopped;
            IsPausing = false; // Reset pausing flag
            UpdateProgress();
            BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", "Job stopped by user.", 0, 0, NumberFilesLeftToDo);
            return true;
        }
        return false;
    }


    /// <summary>
    /// Executes the backup job asynchronously.
    /// This method handles the main backup logic, including file processing,
    /// checking for blocking software, and updating job state.
    /// It will run until the job is either paused, stopped, or fails.
    /// CancellationToken can be used to stop the job externally.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the job externally.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(SourceDirectory)) throw new DirectoryNotFoundException($"Source directory '{SourceDirectory}' does not exist.");
        // if target directory does not exist, create it
        if (!Directory.Exists(TargetDirectory))
        {
            try
            {
                Directory.CreateDirectory(TargetDirectory);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating target directory: {ex.Message}";
                State = JobStates.Failed;
                return;
            }
        }

        // Reset IsPausing flag at the start of execution
        IsPausing = false;
        if (State != JobStates.Paused) await UpdateFilesCountInternalAsync();


        if (FilesToBackup.Count == 0 && TotalFilesToCopy > 0)
        {
            State = JobStates.Finished;
            UpdateProgress();
            return;
        }
        if (TotalFilesToCopy == 0)
        {
            State = JobStates.Finished;
            UpdateProgress();
            return;
        }


        State = JobStates.Working;
        ErrorMessage = null; // Clear previous errors

        var settings = Settings.LoadSettings();
        var priorityExtensions = settings.PriorityFileExtensions ?? new List<string>();
        var encryptionExtensions = settings.FileExtensions ?? new List<string>();
        // Using MaxFileTransferSize (in KB) for encryption size limit, converting to Bytes.
        // Assuming 0 means no limit for encryption as well.
        long maxEncryptionFileSizeLimitBytes = settings.MaxFileTransferSize == 0 ? 0 : (long)settings.MaxFileTransferSize * 1024;


        var sortedFilesToBackup = FilesToBackup
            .Select(f => new { Path = f, Priority = priorityExtensions.Contains(Path.GetExtension(f).ToLower()) })
            .OrderByDescending(f => f.Priority)
            .Select(f => f.Path)
            .ToList();

        long currentJobTotalSizeCopiedThisSession = 0;
        long currentJobTotalFilesCopiedThisSession = 0;


        foreach (var sourceFilePath in sortedFilesToBackup)
        {
            if (CheckCancellationRequested(cancellationToken))
            {
                throw new OperationCanceledException("Job execution cancelled by user.");
            }
            // Check for blocked software before each file
            if (_isSoftwareRunning())
            {
                State = JobStates.Stopped;
                ErrorMessage = "Backup paused due to running blocked software.";
                _addToBlockedJobs(this);
                BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", ErrorMessage ?? "Blocked software running", 0, 0, NumberFilesLeftToDo);
                return;
            }

            var targetFilePath = Path.Combine(TargetDirectory, Path.GetRelativePath(SourceDirectory, sourceFilePath));
            var targetFileDir = Path.GetDirectoryName(targetFilePath);
            if (targetFileDir != null && !Directory.Exists(targetFileDir))
            {
                Directory.CreateDirectory(targetFileDir);
            }

            // Get FileInfo and file size
            var fileInfoForCurrentFile = new FileInfo(sourceFilePath);
            long currentFileSize = fileInfoForCurrentFile.Length; // Declare and assign

            // Initialize a flag for ViewModel interaction for this file iteration
            bool transferRegisteredWithViewModel = false;

            // NEW CHECK: If file exceeds MaxFileTransferSize (converted to bytes in maxEncryptionFileSizeLimitBytes), skip transfer
            // maxEncryptionFileSizeLimitBytes is settings.MaxFileTransferSize * 1024.
            // If settings.MaxFileTransferSize is 0, maxEncryptionFileSizeLimitBytes is 0, meaning no limit.
            if (maxEncryptionFileSizeLimitBytes > 0 && currentFileSize > maxEncryptionFileSizeLimitBytes)
            {
                BackupJobLogger?.LogBackupDetails(Name, "FileSkipInfo", $"File {Path.GetFileName(sourceFilePath)} ({currentFileSize / 1024}KB) exceeds configured MaxFileTransferSize ({settings.MaxFileTransferSize}KB). Skipped transfer.", (ulong)currentFileSize, 0, NumberFilesLeftToDo);
                ErrorMessage = $"Some files were skipped due to size limits.";
                continue; // Skip to the next file
            }

            try
            {
                // currentFileSize is already set from above.

                if (_mainViewModel != null)
                {
                    while (!_mainViewModel.CanTransferFile(currentFileSize) && !cancellationToken.IsCancellationRequested)
                    {
                        BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", $"Waiting for transfer capacity for file: {Path.GetFileName(sourceFilePath)}. Current global load: {_mainViewModel.CurrentGlobalTransferringSizeInBytes / 1024}KB", 0, 0, NumberFilesLeftToDo);
                        State = JobStates.Paused;
                        await Task.Delay(5000, cancellationToken);
                        State = JobStates.Working;
                        // Check for cancellation after delay
                        if (CheckCancellationRequested(cancellationToken))
                        {
                            throw new OperationCanceledException("Job execution cancelled by user.");
                        }
                    }
                    _mainViewModel.IncrementGlobalTransferringSize(currentFileSize);
                    transferRegisteredWithViewModel = true; // Mark that IncrementGlobalTransferringSize was called
                }


                int transferTime = Backup.backupFile(sourceFilePath, targetFilePath);
                if (transferTime < 0)
                {
                    ErrorMessage = $"Error copying file {sourceFilePath} to {targetFilePath}.";
                    BackupJobLogger?.LogBackupDetails(Name, "FileCopyError", ErrorMessage, 0, 0, NumberFilesLeftToDo);
                    State = JobStates.Failed;
                    continue; // Skip to the next file
                }

                string fileExtension = Path.GetExtension(sourceFilePath).ToLower();
                int encryptionResult = 0;
                if (encryptionExtensions.Contains(fileExtension))
                {
                    encryptionResult = EncryptFileIfNeeded(targetFilePath);
                    if (encryptionResult < 0)
                    {
                        ErrorMessage = $"Error encrypting file {targetFilePath}. Encryption error code: {encryptionResult}";
                        BackupJobLogger?.LogBackupDetails(Name, "FileEncryptionError", ErrorMessage, 0, 0, NumberFilesLeftToDo);
                        State = JobStates.Failed;
                        continue; // Skip to the next file
                    }
                }
                // log once the file has been successfully copied
                BackupJobLogger?.LogBackupDetails(Name, sourceFilePath, targetFilePath, (ulong)currentFileSize, transferTime, NumberFilesLeftToDo); // Assuming 0 for transferTime for now

                //remove the file from FilesToBackup and from sortedFilesToBackup
                FilesToBackup.Remove(sourceFilePath);


                TotalFilesCopied++;
                TotalSizeCopied += currentFileSize;
                currentJobTotalFilesCopiedThisSession++;
                currentJobTotalSizeCopiedThisSession += currentFileSize;


                NumberFilesLeftToDo = TotalFilesToCopy - (int)TotalFilesCopied;

                UpdateProgress();

                // Removed call to missing LogProgress
            }
            catch (OperationCanceledException)
            {
                // When Stop() is called, _executionCts is cancelled.
                // Stop() itself sets State = JobStates.Stopped and IsPausing = false.
                // CheckCancellationRequested() also sets State = JobStates.Stopped if token is cancelled.
                // Therefore, if this exception is caught due to a Stop operation,
                // the State should already be JobStates.Stopped. We should not change it.
                // Ensure IsPausing is false as Stop() would have set it.
                IsPausing = false;
                UpdateProgress(); // Update progress based on the current state (e.g., Stopped)
                BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", $"Job operation cancelled. Current state: {State}.", 0, 0, NumberFilesLeftToDo);
                throw; // Rethrow the exception
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error copying file {sourceFilePath}: {ex.Message}";
                BackupJobLogger?.LogBackupDetails(Name, "FileCopyError", ErrorMessage, 0, 0, NumberFilesLeftToDo);
            }
            finally
            {
                // Only decrement if Increment was successfully called for this file.
                if (_mainViewModel != null && transferRegisteredWithViewModel)
                {
                    _mainViewModel.DecrementGlobalTransferringSize(currentFileSize);
                }
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            if (TotalFilesCopied >= TotalFilesToCopy)
            {
                State = JobStates.Finished;
                ErrorMessage = null;
            }
            else if (State != JobStates.Failed)
            {
                State = JobStates.Stopped;
                ErrorMessage = ErrorMessage ?? "Job finished with some files potentially not processed.";
            }
        }

        UpdateProgress();
        BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", $"Job {State}.", 0, 0, NumberFilesLeftToDo);

        if (State == JobStates.Finished || State == JobStates.Failed)
        {
            IsPausing = false;
        }
    }


    public async Task Start()
    {
        try
        {
            ErrorMessage = null; // Clear previous errors
            Progress = 0;        // Explicitly reset progress to 0 for UI update
            TotalFilesCopied = 0; // Reset counters that affect progress
            TotalSizeCopied = 0;
            NumberFilesLeftToDo = TotalFilesToCopy; // Reset based on initial scan

            _executionCts?.Dispose();
            _executionCts = new CancellationTokenSource();
            State = JobStates.Working; // Set to Working before ExecuteAsync
            IsPausing = false;
            await ExecuteAsync(_executionCts.Token); // This will call UpdateProgress internally
        }
        catch (OperationCanceledException)
        {
            State = JobStates.Stopped; 
            IsPausing = false;
            BackupJobLogger?.LogBackupDetails(Name, "SystemOperation", "Job start was cancelled.", 0, 0, NumberFilesLeftToDo);
            UpdateProgress(); 
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error starting job: {ex.Message}";
            State = JobStates.Failed;
            IsPausing = false;
            UpdateProgress(); 
        }
    }

    public void Stop()
    {
        if (State != JobStates.Working && State != JobStates.Paused)
        {
            return;
        }

        _executionCts?.Cancel();
        State = JobStates.Stopped;
        IsPausing = false;
        // Reset progress-related fields when stopping
        Progress = 0;
        TotalFilesCopied = 0;
        TotalSizeCopied = 0;
        // NumberFilesLeftToDo will be updated by UpdateFilesCountInternalAsync if the job is restarted
        // For a stopped job, it's reasonable to show 0 progress.
        // Or, we could preserve the last known NumberFilesLeftToDo if that's preferred for a stopped state.
        // For now, resetting progress to 0 implies all progress is nullified on stop.
        UpdateProgress(); // Ensure progress UI updates to reflect the stopped state
    }

    public void Pause()
    {
        if (State != JobStates.Working)
        {
            ErrorMessage = $"Cannot pause job in state {State}";
            return;
        }
        // Set IsPausing flag to true - this will signal the job to pause after the current file completes
        IsPausing = true;
    }

    public async Task Resume()
    {
        if (State != JobStates.Paused)
        {
            ErrorMessage = $"Cannot resume job in state {State}";
            return;
        }

        try
        {
            if (_executionCts == null || _executionCts.IsCancellationRequested)
            {
                _executionCts?.Dispose();
                _executionCts = new CancellationTokenSource();
            }
            IsPausing = false;
            await ExecuteAsync(_executionCts.Token);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error resuming job: {ex.Message}";
            State = JobStates.Failed;
            IsPausing = false;
        }
    }
}