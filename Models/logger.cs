using System;
using System.Collections.Generic;
using System.IO;


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
        if (!string.IsNullOrEmpty(logDirectory))
        {
            // create the directory if it doesn't exist
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
            LogDirectory = logDirectory;

            StateLogFilePath = Path.Combine(LogDirectory, "state.json");
        }
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
    public void UpdateAllJobsState()
    {
        Console.WriteLine("Updating all jobs state...");
        lock (logLock)
        {
            // Access jobs list from Controller class
            var jobs = Controller.RetrieveBackupJobs();
            
            // Start JSON array with proper indentation
            var state = "[\n";
            
            // Add each job's state
            for (int i = 0; i < jobs.Count; i++)
            {
                state += jobs[i].ToString();
                
                // Add comma after each job except the last one
                if (i < jobs.Count - 1)
                {
                    state += ",\n";
                }
                else
                {
                    state += "\n";
                }
            }
            
            // Close JSON array
            state += "]";
            Console.WriteLine("Saving state to file: " + StateLogFilePath);
            // Write to state file
            File.WriteAllText(StateLogFilePath, state);
        }
    }
}
