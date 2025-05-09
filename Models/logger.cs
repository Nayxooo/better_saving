using System;
using System.Collections.Generic;
using System.IO;


public class Logger
{
    private string LogDirectory = ""; // Directory where logs are stored
    private string DailyLogFilePath = ""; // log file path for the current day
    private string StateLogFilePath = ""; // log the current state of all backup jobs
    private static readonly object logLock = new(); // Lock object for thread safety

    public Logger(string logDirectory)
    {
        LogDirectory = logDirectory;
        StateLogFilePath = Path.Combine(LogDirectory, "state.json");
    }

    // Method to get the log directory path
    public string GetLogDirectory()
    {
        return LogDirectory;
    }

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
