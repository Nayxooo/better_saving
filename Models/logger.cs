using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models; // For JobType and JobState

public class Logger
{
    private string LogDirectory = ""; // Directory where logs are stored
    private string DailyLogFilePath = ""; // log file path for the current day
    private string StateLogFilePath = ""; // log the current state of all backup jobs
    private static readonly object logLock = new(); // Lock object for thread safety

    /// <summary>
    /// Initializes a new logger instance with the specified log directory.
    /// </summary>
    /// <param name="logDirectory">The directory where log files will be stored.</param>
    public Logger(string logDirectory)
    {
        LogDirectory = logDirectory;
        StateLogFilePath = Path.Combine(LogDirectory, "state.json");
    }

    /// <summary>
    /// Gets the directory path where logs are stored.
    /// </summary>
    /// <returns>The log directory path.</returns>
    public string GetLogDirectory()
    {
        return LogDirectory;
    }

    /// <summary>
    /// Logs details of a backup operation to the daily log file.
    /// Uses a thread-safe mechanism to prevent concurrent write operations.
    /// </summary>
    /// <param name="timestamp">The timestamp of the backup operation.</param>
    /// <param name="jobName">The name of the backup job.</param>
    /// <param name="sourceFile">The source file path.</param>
    /// <param name="targetFile">The target file path.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
    public void LogBackupDetails(string timestamp, string jobName, string sourceFile, string targetFile, ulong fileSize, int transferTime)
    {
        lock (logLock)
        {
            DailyLogFilePath = Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");

            string logEntry = $"{timestamp} | '{jobName}' - '{sourceFile}' - '{targetFile}' - '{fileSize}' - '{transferTime}";
            
            try
            {
                // Ensure the log directory exists
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                
                // Append to existing file or create new one
                File.AppendAllText(DailyLogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                return;
            }
        }
    }

    /// <summary>
    /// Updates the state.json file with the current state of all backup jobs.
    /// This provides persistence of job information between application runs.
    /// </summary>
    public void UpdateAllJobsState(List<backupJob> jobs)
    {
        Console.WriteLine("Updating all jobs state...");
        lock (logLock)
        {
            // Start JSON array with proper indentation
            var stateEntries = new List<object>();
            foreach (var job in jobs)
            {
                stateEntries.Add(new
                {
                    Name = job.Name,
                    SourceDirectory = job.GetSourceDirectory(),
                    TargetDirectory = job.GetTargetDirectory(),
                    Type = job.GetJobType().ToString(), // Enum to string
                    State = job.GetState().ToString(),   // Enum to string
                    TotalFilesToCopy = job.GetTotalFilesToCopy(),
                    TotalFilesSize = job.GetTotalFilesSize(), // Assuming this getter exists
                    NumberFilesLeftToDo = job.GetNumberFilesLeftToDo(),
                    Progress = job.GetProgress(),
                    ErrorMessage = job.GetErrorMessage()
                });
            }

            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                // Serialize the list of job states to JSON
                string jsonState = JsonSerializer.Serialize(stateEntries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateLogFilePath, jsonState);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing state log: {ex.Message}");
                // Optionally, rethrow or handle more gracefully
            }
        }
    }

    /// <summary>
    /// Loads backup job configurations from the state.json file.
    /// </summary>
    /// <returns>A list of backupJob objects.</returns>
    public List<backupJob> LoadJobsFromStateFile()
    {
        var jobs = new List<backupJob>();
        if (!File.Exists(StateLogFilePath))
        {
            Console.WriteLine("State file not found. No jobs loaded.");
            return jobs; // Return empty list if state file doesn't exist
        }

        try
        {
            string jsonContent = File.ReadAllText(StateLogFilePath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return jobs; // Return empty list if state file is empty
            }

            var jobStates = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

            if (jobStates == null) return jobs;

            foreach (var jobStateElement in jobStates)
            {
                try
                {
                    string name = jobStateElement.GetProperty("Name").GetString() ?? "";
                    string sourceDir = jobStateElement.GetProperty("SourceDirectory").GetString() ?? "";
                    string targetDir = jobStateElement.GetProperty("TargetDirectory").GetString() ?? "";
                    string typeStr = jobStateElement.GetProperty("Type").GetString() ?? JobType.Full.ToString();
                    JobType type = Enum.Parse<JobType>(typeStr, true);
                    
                    // Create the job instance. The constructor of backupJob might need adjustment
                    // if it requires a Logger instance directly, or if state/progress needs to be set.
                    // For now, assuming a constructor that matches:
                    // public backupJob(string name, string sourceDir, string targetDir, JobType type, int idleTime, Logger logger)
                    // We pass 'this' as the logger.
                    var job = new backupJob(name, sourceDir, targetDir, type, this);

                    // Restore additional properties if needed and if backupJob setters allow
                    // For example, if State and Progress should be restored:
                    // string jobStatusStr = jobStateElement.GetProperty("State").GetString() ?? JobState.Idle.ToString();
                    // job.State = Enum.Parse<JobState>(jobStatusStr, true); // Assuming State has a public setter or internal logic
                    // job.Progress = (byte)(jobStateElement.GetProperty("Progress").GetInt32()); // Assuming Progress has a public setter

                    jobs.Add(job);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing a job from state file: {ex.Message}");
                    // Continue loading other jobs
                }
            }
        }
        catch (JsonException jsonEx)
        {
            Console.Error.WriteLine($"Error deserializing state file: {jsonEx.Message}");
            // Decide if to return empty list or throw
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred while loading jobs from state file: {ex.Message}");
            // Decide if to return empty list or throw
        }
        return jobs;
    }
}
