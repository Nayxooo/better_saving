using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Models;


// Job class to store job information
public class backupJob // If this class needs to notify changes, it should implement INotifyPropertyChanged
{
    public string Name { get; set; }
    private string SourceDirectory { get; set; }
    private string TargetDirectory { get; set; }
    private JobType Type { get; set; }
    
    private JobState _state;
    public JobState State // Made public for ViewModel binding and direct updates
    { 
        get { return _state; }
        set 
        { 
            if (_state != value)
            {
                _state = value;
                // Consider how to notify ViewModel: either this class implements INotifyPropertyChanged,
                // or ViewModel subscribes to an event, or ViewModel polls.
                // For simplicity in this step, direct update from ViewModel or via ExecuteAsync's outcome.
            }
        }
    }
    
    private int TotalFilesToCopy { get; set; }
    private ulong TotalFilesSize { get; set; }
    private int NumberFilesLeftToDo { get; set; }
    private List<string> FilesToBackup { get; set; } = new List<string>(); // Initialize
    
    private int IdleTime { get; set; } = 100; // Default idle time

    private byte _progress;
    public byte Progress // Made public for ViewModel binding and direct updates
    { 
        get { return _progress; }
        set 
        {
            if (_progress != value)
            {
                _progress = value;
                // Similar to State, consider notification mechanism.
            }
        } 
    }
    
    public string? ErrorMessage { get; set; } // Made public
    private readonly Logger? BackupJobLogger; // Nullable if a job might not have its own logger

    // Constructor
    public backupJob(string name, string sourceDir, string targetDir, JobType type, Logger logger)
    {
        Name = name;
        SourceDirectory = sourceDir;
        TargetDirectory = targetDir;
        BackupJobLogger = logger;
        Type = type;
        State = JobState.Idle;
        Progress = 0;
        // Initialize BackupJobLogger if a separate log file per job is desired
        // For now, let's assume job-specific logging might be simpler or handled by the main app logger.
        // If each job needs its own Logger instance:
        // BackupJobLogger = new Logger(Path.GetDirectoryName(jobLogFilePath), Path.GetFileName(jobLogFilePath));
        // For this refactoring, we'll assume detailed file copy logs go through the applicationLogger passed to ExecuteAsync.
        
        InitializeJobDetails();
    }
    
    // Parameterless constructor for JSON deserialization if needed, ensure properties are handled.
    public backupJob() {
        // Initialize with default or indicate they need to be set, e.g. by deserializer
        Name = string.Empty; 
        SourceDirectory = string.Empty;
        TargetDirectory = string.Empty;
        Type = JobType.Full; // Default type
        State = JobState.Idle;
        FilesToBackup = new List<string>();
        // BackupJobLogger can be null if not always used or initialized later
    }


    public string GetSourceDirectory() => SourceDirectory;
    public string GetTargetDirectory() => TargetDirectory;
    public JobType GetJobType() => Type;
    public JobState GetState() => State; // Public property 'State' can be used directly
    public int GetProgress() => Progress; // Public property 'Progress' can be used directly
    public string GetErrorMessage() => ErrorMessage ?? "";
    public int GetTotalFilesToCopy() => TotalFilesToCopy;
    public ulong GetTotalFilesSize() => TotalFilesSize;
    public int GetNumberFilesLeftToDo() => NumberFilesLeftToDo;

    private void InitializeJobDetails()
    {
        try
        {
            FilesToBackup.Clear();
            TotalFilesToCopy = 0;
            TotalFilesSize = 0;
            NumberFilesLeftToDo = 0;

            if (!Directory.Exists(SourceDirectory))
            {
                State = JobState.Failed;
                ErrorMessage = $"Source directory '{SourceDirectory}' not found.";
                return;
            }

            var files = Directory.GetFiles(SourceDirectory, "*.*", SearchOption.AllDirectories);
            FilesToBackup.AddRange(files);
            TotalFilesToCopy = files.Length;
            NumberFilesLeftToDo = TotalFilesToCopy;
            TotalFilesSize = (ulong)files.Sum(f => new FileInfo(f).Length);
            
            if (TotalFilesToCopy == 0)
            {
                State = JobState.Finished; // Or Idle if no files means nothing to do.
                Progress = 100;
            }
            else
            {
                State = JobState.Idle;
                Progress = 0;
            }
        }
        catch (Exception ex)
        {
            State = JobState.Failed;
            ErrorMessage = $"Error initializing job details: {ex.Message}";
        }
    }


    /// <summary>
    /// Asynchronously executes the backup job.
    /// </summary>
    /// <param name="applicationLogger">The main application logger for general logging.</param>
    /// <param name="onProgressChanged">Action to report progress changes.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public async Task ExecuteAsync(Logger applicationLogger, Action<int> onProgressChanged, CancellationToken cancellationToken)
    {
        State = JobState.Working;
        ErrorMessage = null;
        onProgressChanged(Progress); // Initial progress

        try
        {
            if (!Directory.Exists(SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory '{SourceDirectory}' not found.");
            }
            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
            }

            // Re-evaluate files to backup, especially for differential.
            // For simplicity, this example re-evaluates all files. A more robust diff backup
            // would compare against a manifest or last backup state.
            InitializeJobDetails(); // Recalculate file list and sizes.
            if (State == JobState.Failed) // If InitializeJobDetails failed
            {
                 throw new InvalidOperationException(ErrorMessage ?? "Failed to initialize job details for execution.");
            }
            if (TotalFilesToCopy == 0)
            {
                State = JobState.Finished;
                Progress = 100;
                onProgressChanged(Progress);
                applicationLogger.UpdateAllJobsState(new List<backupJob> { this }); // Update its own state
                return;
            }


            NumberFilesLeftToDo = TotalFilesToCopy; // Reset counter

            foreach (var sourceFilePath in new List<string>(FilesToBackup)) // Iterate over a copy in case FilesToBackup is modified
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = Path.GetRelativePath(SourceDirectory, sourceFilePath);
                string targetFilePath = Path.Combine(TargetDirectory, relativePath);

                // Ensure target subdirectory exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);

                bool shouldCopy = true;
                if (Type == JobType.Diff && File.Exists(targetFilePath))
                {
                    // Basic diff: copy if source is newer or sizes differ.
                    // A more robust diff would use hashing or compare against a state file.
                    var sourceInfo = new FileInfo(sourceFilePath);
                    var targetInfo = new FileInfo(targetFilePath);
                    if (sourceInfo.LastWriteTimeUtc <= targetInfo.LastWriteTimeUtc && sourceInfo.Length == targetInfo.Length)
                    {
                        shouldCopy = false;
                    }
                }

                if (shouldCopy)
                {
                    int timeTaken = Backup.backupFile(sourceFilePath, targetFilePath); // Corrected: Removed 'Models.' prefix, assuming Backup class is in the same global namespace or accessible via using static.
                    if (timeTaken >= 0)
                    {
                        applicationLogger.LogBackupDetails(
                            DateTime.Now.ToString("o"), // ISO 8601 format
                            Name,
                            sourceFilePath,
                            targetFilePath,
                            (ulong)new FileInfo(sourceFilePath).Length,
                            timeTaken
                        );
                    }
                    else
                    {
                        // Log failure for this specific file, but continue if possible?
                        // Or throw to fail the whole job. For now, let's assume we log and continue.
                        // This part needs more robust error handling strategy.
                        Console.WriteLine($"Failed to copy {sourceFilePath}"); // Placeholder for more robust logging
                    }
                }
                
                NumberFilesLeftToDo--;
                UpdateProgress();
                onProgressChanged(Progress); // Report progress
                
                // Simulate work or allow other tasks to run
                await Task.Delay(IdleTime > 0 ? IdleTime : 1, cancellationToken); 
            }

            State = JobState.Finished;
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            State = JobState.Stopped;
            ErrorMessage = "Job was cancelled by the user.";
        }
        catch (Exception ex)
        {
            State = JobState.Failed;
            ErrorMessage = $"Job execution failed: {ex.Message}";
            // applicationLogger.LogCriticalEvent($"Job '{Name}' failed: {ex.ToString()}"); // Example of more detailed logging
        }
        finally
        {
            onProgressChanged(Progress); // Final progress update
            // The MainViewModel will call UpdateAllJobsState after ExecuteAsync completes.
        }
    }

    /// <summary>
    /// Updates the progress percentage based on the number of files left to do.
    /// The progress is calculated as the ratio of completed files to total files.
    /// If all files are copied, the state is set to Finished.
    /// </summary>
    private void UpdateProgress()
    {
        if (TotalFilesToCopy == 0)
        {
            Progress = 100;
        }
        else
        {
            Progress = (byte)(((TotalFilesToCopy - NumberFilesLeftToDo) * 100) / TotalFilesToCopy);
        }

        if (NumberFilesLeftToDo == 0 && TotalFilesToCopy > 0) // Ensure it was not 0 from start
        {
            // State will be set to Finished in ExecuteAsync upon successful completion of all files.
            // This method just updates the Progress byte.
        }
    }
}