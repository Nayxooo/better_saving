using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


// Job class to store job information
public class backupJob
{
    public string Name { get; set; }
    private string SourceDirectory { get; set; }
    private string TargetDirectory { get; set; }
    private JobType Type { get; set; }
    
    // Modified State property with automatic update
    private jobState _state;
    private jobState State 
    { 
        get { return _state; }
        set 
        { 
            // Only update and trigger logger if state is actually changing
            if (_state != value)
            {
                _state = value;
                // Update the state in the logger whenever it changes
                BackupJobLogger?.UpdateAllJobsState();
            }
        }
    }
    
    private int TotalFilesToCopy { get; set; } // Total number of files in the source directory and subdirectories
    private ulong TotalFilesSize { get; set; } // total size of the files in the source directory and subdirectories (in bytes)
    private int NumberFilesLeftToDo { get; set; } // Number of files not backed up yet
    private List<string> FilesToBackup { get; set; } // List of file paths that need to be backed up
    
    private int IdleTime { get; set; } // Time to wait when no files are left to backup (in milliseconds)

    // Modified Progress property with automatic update
    private byte _progress;
    private byte Progress 
    { 
        get { return _progress; }
        set 
        {
            if (_progress != value)
            {
                _progress = value;
                // Update the state in the logger whenever progress changes
                BackupJobLogger?.UpdateAllJobsState();
            }
        } 
    }
    
    private string? ErrorMessage { get; set; } // Error message if any error occurs
    private Logger BackupJobLogger;

    // Public accessor methods for the GUI
    public string GetSourceDirectory() => SourceDirectory;
    public string GetTargetDirectory() => TargetDirectory;
    public JobType GetJobType() => Type;
    public jobState GetState() => State;
    public int GetProgress() => Progress;
    public string GetErrorMessage() => ErrorMessage ?? "";
    public int GetTotalFilesToCopy() => TotalFilesToCopy;
    public int GetNumberFilesLeftToDo() => NumberFilesLeftToDo;

    private void UpdateProgress()
    {
        // Calculate the progress percentage
        Progress = (byte)((TotalFilesToCopy - NumberFilesLeftToDo) * 100 / Math.Max(1, TotalFilesToCopy));
        if (Progress == 100)
        {
            State = jobState.Finished; // Set state to Finished when progress is 100%
        }
    }

    public backupJob(string name, string sourceDir, string targetDir, JobType type, int idleTime, Logger loggerInstance)
    {
        // check if the source directory exists
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDir}' does not exist.");
        }
        

        Name = name;
        SourceDirectory = sourceDir;
        TargetDirectory = targetDir;
        Type = type;
        State = jobState.Stopped;
        IdleTime = idleTime;
        BackupJobLogger = loggerInstance;
        FilesToBackup = [];
        
        Progress = 0;
    }

    public void UpdateFilesCount()
    {
        Console.WriteLine($"Updating files count for job '{Name}'...");
        // Clear the existing list of files to backup
        FilesToBackup.Clear();
        
        // Count the total number of files in the source directory (including all subdirectories)
        var files = Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories);
        TotalFilesToCopy = files.Length;

        // Calculate the total size of the files
        TotalFilesSize = 0;
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            TotalFilesSize += (ulong)fileInfo.Length;
        }

        // Calculate the number of files that need to be copied and build the list
        NumberFilesLeftToDo = 0;
        foreach (var file in files)
        {
            // Determine the relative path of the file
            var relativePath = Path.GetRelativePath(SourceDirectory, file);
            var targetFilePath = Path.Combine(TargetDirectory, relativePath);
            
            bool needsCopy = false;
            if (!File.Exists(targetFilePath) || Type == JobType.Full)
            {
                needsCopy = true; // File doesn't exist in target, so it needs to be copied
            }
            else if (Type == JobType.Diff)
            {
                // For Diff backup, check if the file has changed
                string sourceFileHash = Hashing.GetFileHash(file);
                string targetFileHash = Hashing.GetFileHash(targetFilePath);
                // if any of the hashs are empty -> error accessing the file
                if (string.IsNullOrEmpty(sourceFileHash) || string.IsNullOrEmpty(targetFileHash))
                {
                    ErrorMessage = $"Error accessing file during Hashing: {file}";
                    State = jobState.Failed;
                    return;
                }

                needsCopy = Hashing.GetFileHash(file) != Hashing.GetFileHash(targetFilePath);
            }

            if (needsCopy)
            {
                NumberFilesLeftToDo++;
                FilesToBackup.Add(file); // Add the file path to the list of files to backup
                UpdateProgress();
            }
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        State = jobState.Working;
        UpdateFilesCount();

        try
        {
            while (State != jobState.Stopped && !cancellationToken.IsCancellationRequested)
            {
                if (NumberFilesLeftToDo > 0)
                {
                    State = jobState.Working;
                    // Process files from the FilesToBackup list
                    foreach (var file in FilesToBackup.ToArray()) // Using ToArray to create a copy of the list to avoid modification during iteration
                    {
                        // Check for cancellation
                        if (cancellationToken.IsCancellationRequested)
                        {
                            State = jobState.Stopped;
                            return;
                        }

                        // Determine the relative path of the file
                        var relativePath = Path.GetRelativePath(SourceDirectory, file);
                        var targetFilePath = Path.Combine(TargetDirectory, relativePath);
                        
                        // Ensure the target directory exists
                        string? FileTargetDirectory = Path.GetDirectoryName(targetFilePath);
                        if (FileTargetDirectory != null)
                        {
                            Directory.CreateDirectory(FileTargetDirectory);
                        }
                        else
                        {
                            ErrorMessage = $"Error creating target directory for file: {file}";
                            State = jobState.Failed;
                            return;
                        }
                        
                        Console.WriteLine($"\nCopying file: {file} to {targetFilePath}...");
                        
                        // Copy the file
                        int timeElapsed = await Task.Run(() => Backup.backupFile(file, targetFilePath));
                        
                        // Log the backup details
                        string timestamp = DateTime.Now.ToString("o");
                        ulong fileSize = (ulong)new FileInfo(file).Length;
                        BackupJobLogger.LogBackupDetails(timestamp, Name, file, targetFilePath, fileSize, timeElapsed);
                        
                        if (timeElapsed == -1)
                        {
                            ErrorMessage = $"Error copying file: {file}";
                            State = jobState.Failed;
                            return;
                        }

                        NumberFilesLeftToDo--;
                        UpdateProgress();
                    }
                }
                else
                {
                    Console.WriteLine($"No files to backup for job '{Name}'. Waiting 5 minutes before checking again...");
                    try
                    {
                        State = jobState.Idle;
                        // Wait asynchronously for 5 minutes (300,000 milliseconds)
                        await Task.Delay(300000, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Wait was canceled.");
                        State = jobState.Stopped;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sleep interrupted: {ex.Message}");
                    }
                }

                // Only update if not stopped
                if (State != jobState.Stopped && !cancellationToken.IsCancellationRequested)
                {
                    // Update the file count to check if there are any files left to do
                    UpdateFilesCount();
                }
            }
            State = jobState.Stopped;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error executing backup: {ex.Message}";
            State = jobState.Failed;
        }
    }
    
    // Keep the synchronous method for backwards compatibility
    public void Execute()
    {
        // Run the async method synchronously
        ExecuteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public override string ToString()
    {
        // Escape backslashes in file paths for proper JSON formatting
        string escapedSourceDir = SourceDirectory.Replace("\\", "\\\\");
        string escapedTargetDir = TargetDirectory.Replace("\\", "\\\\");

        // double the \ in the error message
        string escapedErrorMessage = ErrorMessage?.Replace("\\", "\\\\") ?? "";
        
        return string.Format(@"    {{
        ""Name"": ""{0}"",
        ""SourceDirectory"": ""{1}"",
        ""TargetDirectory"": ""{2}"",
        ""Type"": ""{3}"",
        ""State"": ""{4}"",
        ""ErrorMessage"": ""{5}"",
        ""TotalFilesToCopy"": {6},
        ""TotalFilesSize"": {7},
        ""NumberFilesLeftToDo"": {8},
        ""Progress"": {9}
    }}", Name, escapedSourceDir, escapedTargetDir, Type, State, escapedErrorMessage, TotalFilesToCopy, TotalFilesSize, NumberFilesLeftToDo, Progress);
    }
}