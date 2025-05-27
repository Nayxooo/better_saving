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

    private readonly List<string> FilesToBackup  = [];

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
    public backupJob(string name, string sourceDir, string targetDir, JobType type, Logger? loggerInstance, Func<bool>? isSoftwareRunning = null, Action<backupJob>? addToBlockedJobs = null)
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

    public async Task UpdateFilesCountAsync() // Public method to allow external refresh if needed, now async
    {
        await UpdateFilesCountInternalAsync();
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
        if (!Directory.Exists(TargetDirectory)) throw new DirectoryNotFoundException($"Target directory '{TargetDirectory}' does not exist.");
        try
        {
            if (State == JobStates.Stopped) await UpdateFilesCountInternalAsync(); // Initial scan or rescan if the job was stopped, now async
            State = JobStates.Working;

            while (State != JobStates.Paused && State != JobStates.Failed && !cancellationToken.IsCancellationRequested)
            {
                if (State == JobStates.Finished && NumberFilesLeftToDo == 0) // If it was set to finished and no files left
                {
                    State = JobStates.Stopped; // For continuous backup, go to Stopped then rescan after delay
                    // If it becomes Stopped, the outer while loop condition (State != Paused) might still be true.
                    // It should re-evaluate after this. If it's stopped, it might need to re-scan or wait.
                    // The current logic will call UpdateFilesCountInternalAsync at the end of the loop if state is working.
                    // If state becomes stopped, and then the while loop continues, it might try to process with an empty list.
                    // Let's ensure if it's stopped, it effectively waits for the next cycle or a start command.
                    // The current continuous backup logic (rescan at end of while) handles this.
                    await Task.Delay(IdleTime, cancellationToken); // Wait before rescanning if it was finished
                    await UpdateFilesCountInternalAsync(); // Rescan
                    if (NumberFilesLeftToDo == 0 && TotalFilesToCopy > 0) State = JobStates.Finished; // Remain finished if still no files after rescan
                    else if (TotalFilesToCopy == 0) State = JobStates.Finished; // No files to backup
                    else State = JobStates.Working; // Ready for next batch
                    continue;
                }

                State = JobStates.Working; // Ensure state is working if there are files or if recovering from Finished->Stopped

                // Get priority file extensions from settings
                var settings = Settings.LoadSettings();
                var priorityExtensions = settings.PriorityFileExtensions;

                // Prepare the list of files to process for this iteration
                var currentFilesToBackupArray = FilesToBackup.ToArray();
                if (priorityExtensions.Count > 0)
                {
                    currentFilesToBackupArray = [.. currentFilesToBackupArray
                        .OrderByDescending(file =>
                            priorityExtensions.Contains(Path.GetExtension(file).ToLower()))];
                }

                // Determine how many files from this batch have already been processed
                // This allows resuming from where it left off if ExecuteAsync is re-entered after a pause.
                int filesProcessedInThisBatch = FilesToBackup.Count - NumberFilesLeftToDo;
                if (filesProcessedInThisBatch < 0) filesProcessedInThisBatch = 0; // Should not happen with current logic

                var filesToIterate = currentFilesToBackupArray.Skip(filesProcessedInThisBatch);

                foreach (var file in filesToIterate)
                {
                    // Check for cancellation request (complete stop)
                    if (cancellationToken.IsCancellationRequested)
                    {
                        State = JobStates.Paused;
                        IsPausing = false;
                        return;
                    }

                    // Check for blocking software - this loop waits for the current file
                    while (_isSoftwareRunning() && !cancellationToken.IsCancellationRequested)
                    {
                        if (State != JobStates.Paused) // Set to Paused and log only once per blocking incident
                        {
                            // IsPausing = true; // Visual hint that it's pausing due to software
                            State = JobStates.Paused;
                            _addToBlockedJobs(this); // Add to BlockedJobs
                            BackupJobLogger?.LogBackupDetails(Name, "System", "Paused due to blocked software", 0, 0, 0);
                        }
                        await Task.Delay(1000, cancellationToken); // Wait for software to close
                    }

                    if (cancellationToken.IsCancellationRequested) // Check again after potential delay
                    {
                        State = JobStates.Paused;
                        IsPausing = false;
                        return;
                    }

                    // If it was paused due to software, and software is now closed, resume to Working for this file
                    if (State == JobStates.Paused) // Implies _isSoftwareRunning was true and now is false
                    {
                        State = JobStates.Working;
                        // IsPausing = false; // Reset visual hint if it was set by this block
                    }
                    // Ensure IsPausing reflects the external Pause() command's state if any,
                    // not just the temporary state from blocking software.
                    // The Pause() method sets _isPausing, Resume() clears it.
                    // The blocking software logic above should not permanently change _isPausing if an external pause is active.
                    // For simplicity, the blocking software pause is self-contained. If an external _isPausing is true, it will be caught later.


                    var relativePath = Path.GetRelativePath(SourceDirectory, file);
                    var targetFilePath = Path.Combine(TargetDirectory, relativePath);
                    string? fileTargetDirectory = Path.GetDirectoryName(targetFilePath);

                    if (fileTargetDirectory != null)
                    {
                        Directory.CreateDirectory(fileTargetDirectory);
                    }
                    else
                    {
                        ErrorMessage = $"Error creating target directory for file: {file}";
                        State = JobStates.Failed;
                        return;
                    }

                    int timeElapsed = -1;
                    ulong fileSize = 0;
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        fileSize = (ulong)fileInfo.Length;
                        timeElapsed = await Task.Run(() => Backup.backupFile(file, targetFilePath), cancellationToken);
                        BackupJobLogger?.UpdateAllJobsState();
                    }
                    catch (Exception ex) // Catch errors during backupFile or FileInfo
                    {
                        ErrorMessage = $"Error processing file {file}: {ex.Message}";
                        State = JobStates.Failed;
                        // Log this error specifically if needed
                        BackupJobLogger?.LogBackupDetails(Name, file, targetFilePath, fileSize, -1, 0);
                        return;
                    }
                    if (timeElapsed == -1)
                    {
                        ErrorMessage = $"Error copying file: {file}";
                        // Adjusted to 6 arguments
                        BackupJobLogger?.LogBackupDetails(Name, file, targetFilePath, fileSize, -1, 0);
                        State = JobStates.Failed;
                        return;
                    }

                    // Encrypt the file if needed and get the exit code
                    int encryptionExitCode = EncryptFileIfNeeded(targetFilePath);

                    BackupJobLogger?.LogBackupDetails(Name, file, targetFilePath, fileSize, timeElapsed, encryptionExitCode);

                    // Successfully copied
                    // Ensure NumberFilesLeftToDo doesn't go below zero
                    if (NumberFilesLeftToDo > 0) NumberFilesLeftToDo--;


                    // Ensure TotalFilesCopied doesn't exceed TotalFilesToCopy
                    if (TotalFilesCopied < TotalFilesToCopy) TotalFilesCopied++;

                    TotalSizeCopied += (long)fileSize;
                    UpdateProgress(); // Update overall progress and potentially state to Finished

                    if (State == JobStates.Finished) // If UpdateProgress set state to Finished
                    {
                        break; // Exit the foreach loop, outer loop will handle Stopped/rescan/idle
                    }

                    // Check if an external pause request (from Pause() method) has been made
                    if (IsPausing)
                    {
                        State = JobStates.Paused; // Set the actual state to Paused
                        // IsPausing remains true, to be reset by Resume() or Stop()
                        BackupJobLogger?.LogBackupDetails(Name, "System", "Paused by user request", 0, 0, 0);
                        return; // Exit ExecuteAsync, job is now paused.
                    }
                }

                // After processing a batch, if not stopped/failed/finished, re-scan for continuous backup.
                if (State == JobStates.Working && !cancellationToken.IsCancellationRequested)
                {
                    // If all files in the current FilesToBackup list were processed (NumberFilesLeftToDo == 0)
                    // or if the list was empty to begin with.
                    if (NumberFilesLeftToDo == 0)
                    {
                        await Task.Delay(IdleTime, cancellationToken); // Wait before rescanning
                        await UpdateFilesCountInternalAsync(); // Check for new files
                        if (NumberFilesLeftToDo == 0 && TotalFilesToCopy > 0) State = JobStates.Finished; // Still no new files to backup but source not empty
                        else if (TotalFilesToCopy == 0) State = JobStates.Finished; // Source is empty
                        // If new files found, State remains Working.
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            State = JobStates.Paused;
            IsPausing = false; // Reset IsPausing flag when job is stopped
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error executing backup: {ex.Message}";
            State = JobStates.Failed;
            IsPausing = false; // Reset IsPausing flag when job fails
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested && State != JobStates.Failed)
            {
                State = JobStates.Paused;
                IsPausing = false; // Ensure IsPausing is reset when job is stopped (cancelled)
            }
            else if (State == JobStates.Working) // If loop terminated while still "Working"
            {
                State = JobStates.Paused;
                IsPausing = false; // If loop exited unexpectedly while 'Working', transition to 'Paused' and reset IsPausing.
            }
        }
    }


    public async Task Start()
    {
        if (State != JobStates.Stopped && State != JobStates.Paused && State != JobStates.Failed)
        {
            ErrorMessage = $"Cannot start job in state {State}";
            return;
        }

        try
        {
            _executionCts?.Dispose();
            _executionCts = new CancellationTokenSource();
            State = JobStates.Working;
            IsPausing = false;
            await ExecuteAsync(_executionCts.Token);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error starting job: {ex.Message}";
            State = JobStates.Failed;
            IsPausing = false;
        }
    }

    public void Stop()
    {
        if (State != JobStates.Working && State != JobStates.Paused)
        {
            ErrorMessage = $"Cannot stop job in state {State}";
            return;
        }

        _executionCts?.Cancel();
        State = JobStates.Paused;
        IsPausing = false;
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
            State = JobStates.Working;
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