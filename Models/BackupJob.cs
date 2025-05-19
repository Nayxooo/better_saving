using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using better_saving.Models; // For JobType, JobStates, Hashing, Backup

// Job class to store job information
public class backupJob : INotifyPropertyChanged
{
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
    public JobStates State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
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

    private List<string> FilesToBackup { get; set; } = new List<string>();

    public int IdleTime { get; set; } = 300000; // 5 minutes default idle time

    private byte _progress;
    public byte Progress
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

    private readonly Logger? BackupJobLogger;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public backupJob(string name, string sourceDir, string targetDir, JobType type, Logger? loggerInstance)
    {
        // Initialize properties first
        Name = name;
        SourceDirectory = sourceDir;
        TargetDirectory = targetDir;
        Type = type;
        State = JobStates.Idle;
        BackupJobLogger = loggerInstance;

        Progress = 0;
        TotalFilesCopied = 0; // Represents files skipped during scan + files copied during execution
        TotalSizeCopied = 0;  // Represents size of files skipped + size of files copied

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
    }

    private async Task UpdateFilesCountInternalAsync()
    {
        // Reset state for the new scan.
        // FilesToBackup is cleared, other counters are reset to accumulate new values.
        FilesToBackup.Clear();
        // If this method is called when a job is already in a terminal state (Failed, Stopped),
        // it might not be desired to change it back to Working.
        // However, typically this is called for Idle jobs or as part of ExecuteAsync.
        // For now, let's assume it's okay to set to Working if not already Failed.
        if (State != JobStates.Failed && State != JobStates.Stopped)
        {
            State = JobStates.Working; // Indicate scanning is in progress
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
            // No files in source, job is effectively finished or idle.
            // UpdateProgress will handle setting state to Finished if appropriate (e.g., if Idle).
            if (State == JobStates.Working) State = JobStates.Idle; // If scanning made it working, but no files.
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

                    if (!File.Exists(targetFilePath) || Type == JobType.Full || (Type == JobType.Diff && !Hashing.CompareFiles(file, targetFilePath)))
                    {
                        // File needs to be backed up
                        FilesToBackup.Add(file); // Directly add to the member list
                    }
                    else
                    {
                        // File is already backed up or skipped for this scan
                        TotalFilesCopied++;         // Directly increment member counter for skipped files
                        TotalSizeCopied += (long)currentFileSize; // Directly increment member counter for size of skipped files
                    }
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

        // If the state was 'Working' due to scan, and scan is done, revert to Idle if no files to backup,
        // or let ExecuteAsync handle it. UpdateProgress will set to Finished if applicable.
        if (State == JobStates.Working)
        {
            // UpdateProgress will set to Finished if TotalFilesCopied == TotalFilesToCopy
            // Otherwise, it remains Working, which is fine if ExecuteAsync is about to run.
            // If UpdateFilesCount is called standalone, and it's done, it should be Idle if there's work, or Finished.
        }
        UpdateProgress(); // Update overall progress and potentially state
    }

    public async Task UpdateFilesCountAsync() // Public method to allow external refresh if needed, now async
    {
        await UpdateFilesCountInternalAsync();
    }

    private void UpdateProgress()
    {
        if (TotalFilesToCopy == 0)
        {
            Progress = 100;
        }
        else
        {
            Progress = (byte)(TotalFilesCopied * 100 / TotalFilesToCopy);
        }

        if (Progress >= 100 && (State == JobStates.Working || State == JobStates.Idle) && TotalFilesToCopy > 0)
        {
            // If all files are copied (TotalFilesCopied == TotalFilesToCopy)
            if (TotalFilesCopied == TotalFilesToCopy)
            {
                State = JobStates.Finished;
            }
        }
        else if (TotalFilesToCopy == 0 && State == JobStates.Idle) // Empty job considered finished
        {
            State = JobStates.Finished;
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        State = JobStates.Working;
        await UpdateFilesCountInternalAsync(); // Initial scan or rescan, now async

        try
        {
            while (State != JobStates.Stopped && State != JobStates.Failed && !cancellationToken.IsCancellationRequested)
            {
                if (NumberFilesLeftToDo == 0)
                {
                    // If all files were processed and it became 0, UpdateProgress might have set it to Finished.
                    // If Finished, we should not go to Idle for continuous backup unless explicitly designed.
                    // For now, replicating console logic: if finished, it will idle and rescan.
                    if (State == JobStates.Finished) // If UpdateProgress set it to Finished
                    {
                        // Behavior for continuous backup: after finishing, go to Idle and wait.
                        State = JobStates.Idle;
                    }

                    if (State == JobStates.Idle)
                    {
                        try
                        {
                            await Task.Delay(IdleTime, cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            State = JobStates.Stopped;
                            return;
                        }
                        if (cancellationToken.IsCancellationRequested) { State = JobStates.Stopped; return; }

                        State = JobStates.Working; // Assume work will be done after delay
                        await UpdateFilesCountInternalAsync(); // Re-scan for changes, now async
                        if (NumberFilesLeftToDo == 0 && State != JobStates.Failed) // If still nothing after re-scan
                        {
                            State = JobStates.Idle; // Go back to idle
                            continue;
                        }
                    }
                }

                if (State == JobStates.Finished && NumberFilesLeftToDo == 0) // If it was set to finished and no files left
                {
                    State = JobStates.Idle; // For continuous backup, go to idle then rescan after delay
                    continue;
                }


                State = JobStates.Working; // Ensure state is working if there are files
                foreach (var file in FilesToBackup.ToArray()) // ToArray for safe iteration if list could change (though it shouldn't here)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        State = JobStates.Stopped;
                        return;
                    }

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
                    }
                    catch (Exception ex) // Catch errors during backupFile or FileInfo
                    {
                        ErrorMessage = $"Error processing file {file}: {ex.Message}";
                        State = JobStates.Failed;
                        // Log this error specifically if needed
                        // Adjusted to 6 arguments: combine error details into a single message if necessary or log separately.
                        BackupJobLogger?.LogBackupDetails(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"), Name, file, targetFilePath, fileSize, -1 /*, $"TaskRun/BackupFile Error: {ex.Message}"*/);
                        return;
                    }


                    if (timeElapsed == -1)
                    {
                        ErrorMessage = $"Error copying file: {file}";
                        // Adjusted to 6 arguments
                        BackupJobLogger?.LogBackupDetails(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"), Name, file, targetFilePath, fileSize, timeElapsed /*, ErrorMessage*/);
                        State = JobStates.Failed;
                        return;
                    }

                    BackupJobLogger?.LogBackupDetails(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"), Name, file, targetFilePath, fileSize, timeElapsed);

                    // Successfully copied
                    NumberFilesLeftToDo--;
                    TotalFilesCopied++;
                    TotalSizeCopied += (long)fileSize;
                    UpdateProgress(); // Update overall progress and potentially state to Finished

                    if (State == JobStates.Finished) // If UpdateProgress set state to Finished
                    {
                        break; // Exit the foreach loop, outer loop will handle idle/rescan
                    }
                }

                // After processing a batch, if not stopped/failed/finished, re-scan for continuous backup.
                if (State == JobStates.Working && !cancellationToken.IsCancellationRequested)
                {
                    await UpdateFilesCountInternalAsync(); // Check for new files that might have appeared, now async
                }
            }
        }
        catch (TaskCanceledException)
        {
            State = JobStates.Stopped;
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
                State = JobStates.Stopped;
                IsPausing = false; // Ensure IsPausing is reset when job is stopped
            }
            else if (State == JobStates.Working) // If loop terminated while still "Working"
            {
                State = JobStates.Stopped;
                IsPausing = false; // Ensure IsPausing is reset when job is stopped
            }
        }
    }

    public void Execute()
    {
        // For synchronous execution, create a CancellationTokenSource if not managed externally for this call
        var cts = new CancellationTokenSource();
        try
        {
            ExecuteAsync(cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Synchronous execution wrapper error: {ex.Message}";
            State = JobStates.Failed;
            // Log or handle as needed
        }
    }

    public override string ToString()
    {
        string escapedSourceDir = SourceDirectory.Replace("\\\\", "\\\\\\\\").Replace("\\", "\\\\");
        string escapedTargetDir = TargetDirectory.Replace("\\\\", "\\\\\\\\").Replace("\\", "\\\\");
        string escapedErrorMessage = ErrorMessage?.Replace("\\\\", "\\\\\\\\").Replace("\\", "\\\\") ?? "";

        return string.Format(@"    {{
        ""Name"": ""{0}"",
        ""SourceDirectory"": ""{1}"",
        ""TargetDirectory"": ""{2}"",
        ""Type"": ""{3}"",
        ""State"": ""{4}"",
        ""ErrorMessage"": ""{5}"",
        ""TotalFilesToCopy"": {6},
        ""TotalFilesCopied"": {7},
        ""TotalSizeToCopy"": {8},
        ""TotalSizeCopied"": {9},
        ""NumberFilesLeftToDo"": {10},
        ""Progress"": {11},
        ""IdleTime"": {12}
    }}", Name, escapedSourceDir, escapedTargetDir, Type, State, escapedErrorMessage, TotalFilesToCopy, TotalFilesCopied, TotalSizeToCopy, TotalSizeCopied, NumberFilesLeftToDo, Progress, IdleTime);
    }
}