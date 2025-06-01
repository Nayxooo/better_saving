using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using better_saving.Models; // For JobType and JobState
using System.Collections.Generic; // Required for IEnumerable
using System; // Required for Func

namespace better_saving.Models
{
    public class Logger
    {
        private readonly string LogDirectory = ""; // Directory where logs are stored
        private string DailyLogFilePath = ""; // log file path for the current day
        private readonly string StateLogFilePath = ""; // log the current state of all backup jobs
        private readonly string DebugFile = Path.Combine(AppContext.BaseDirectory, "logs\\EasySave33_bugReport.log");

        private static readonly object logLock = new(); // Lock object for thread safety
        private Func<IEnumerable<backupJob>>? _jobProvider; // Function to retrieve the current list of backup jobs
        private readonly TCPServer? _tcpServer; // TCPServer instance to handle TCP connections, if provided


        /// <summary>
        /// Initializes a new logger instance.
        /// Log files will be stored in a 'logs' subdirectory next to the application executable.
        /// </summary>
        public Logger(TCPServer? tcpServer = null) // Modified constructor to accept TCPServer
        {
            _tcpServer = tcpServer; // Store the TCPServer instance
                                    // _jobs = jobs; // Removed
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            StateLogFilePath = Path.Combine(LogDirectory, "state.json");
            // Ensure log directory exists on instantiation
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            // delete the bug report file at startup
            if (File.Exists(DebugFile))
            {
                try
                {
                    File.Delete(DebugFile);
                }
                catch (Exception ex)
                {
                    // If deletion fails, log the error to the debug file
                    LogError($"Failed to delete debug file at startup: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Public getter for the state log file path.
        /// </summary>
        public string GetStateLogFilePath()
        {
            return StateLogFilePath;
        }

        public void LogError(string errorMessage)
        {
            lock (logLock)
            {
                String timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
                string logEntry = $"\"{timestamp} | LOGGER_FAILURE: {errorMessage}\"";
                try
                {
                    // Ensure the log directory exists
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // Append to existing file or create new one
                    File.AppendAllText(DebugFile, logEntry + Environment.NewLine);
                }
                catch
                {
                    // If logging fails, we do nothing to avoid infinite loops
                }
            }
        }

        /// <summary>
        /// Sets the provider for retrieving the list of backup jobs.
        /// </summary>
        /// <param name="jobProvider">A function that returns the current list of backup jobs.</param>
        public void SetJobProvider(Func<IEnumerable<backupJob>> jobProvider)
        {
            _jobProvider = jobProvider;
        }

        /// <summary>
        /// Gets the directory path where logs are stored.
        /// </summary>
        /// <returns>The log directory path.</returns>
        public string GetLogDirectory()
        {
            return LogDirectory;
        }    /// <summary>
             /// Logs details of a backup operation to the daily log file.
             /// Uses a thread-safe mechanism to prevent concurrent write operations.
             /// </summary>
             /// <param name="timestamp">The timestamp of the backup operation.</param>
             /// <param name="jobName">The name of the backup job.</param>
             /// <param name="sourceFile">The source file path.</param>
             /// <param name="targetFile">The target file path.</param>
             /// <param name="fileSize">The size of the file in bytes.</param>
             /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
        public void LogBackupDetails(string jobName, string sourceFile, string targetFile, ulong fileSize, int transferTime)
        {
            LogBackupDetails(jobName, sourceFile, targetFile, fileSize, transferTime, 0);
        }

        /// <summary>
        /// Logs details of a backup operation to the daily log file with encryption exit code.
        /// Uses a thread-safe mechanism to prevent concurrent write operations.
        /// </summary>
        /// <param name="timestamp">The timestamp of the backup operation.</param>
        /// <param name="jobName">The name of the backup job.</param>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="targetFilePath">The target file path.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
        /// <param name="encryptionExitCode">The exit code from the encryption process (0 if no encryption was needed).</param>
        public void LogBackupDetails(string jobName, string sourceFilePath, string targetFilePath, ulong fileSize, int transferTime, int encryptionExitCode)
        {
            lock (logLock)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
                DailyLogFilePath = Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

                string logEntry = $"[{timestamp}] [{jobName}] \"{sourceFilePath}\" - \"{targetFilePath}\" - {fileSize} - {transferTime} - {encryptionExitCode}";

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
        public void UpdateAllJobsState(bool SkipNullCheck = false) // Parameter removed in previous step, logic now uses _jobProvider
        {
            Console.WriteLine("Updating all jobs state...");
            LogError($"{_jobProvider?.Invoke()?.Count() ?? 0} jobs to update in state.json");
            if (_jobProvider == null)
            {
                LogError($"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz} | LOGGER_FAILURE in UpdateAllJobsState: Job provider is not set.{Environment.NewLine}");
                return;
            }

            // check if the job provider returns null or empty
            if (!SkipNullCheck && (_jobProvider.Invoke() == null || !_jobProvider.Invoke().Any()))
            {
                LogError($"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz} | LOGGER_FAILURE in UpdateAllJobsState: Job provider returned null or empty job list.{Environment.NewLine}");
                return;
            }

            var jobs = _jobProvider.Invoke();
            lock (logLock)
            {
                try
                {
                    // Create a temporary file path for safe writing
                    string tempFilePath = StateLogFilePath + ".tmp";

                    // Start JSON array with proper indentation
                    var stateEntries = new List<object>();
                    foreach (var job in jobs) // Use the jobs from the provider
                    {
                        stateEntries.Add(new
                        {
                            Name = job.Name,
                            SourceDirectory = job.SourceDirectory,
                            TargetDirectory = job.TargetDirectory,
                            Type = job.Type.ToString(), // Enum to string
                            State = job.State.ToString(),   // Enum to string
                            TotalFilesToCopy = job.TotalFilesToCopy,
                            TotalFilesSize = job.TotalSizeToCopy, // Renamed from TotalFilesSize for consistency
                            NumberFilesLeftToDo = job.NumberFilesLeftToDo,
                            Progress = job.Progress,
                            ErrorMessage = job.ErrorMessage
                        });
                    }

                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // First write to a temporary file
                    string jsonState = JsonSerializer.Serialize(stateEntries, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tempFilePath, jsonState);

                    // If successful, replace the original file
                    if (File.Exists(tempFilePath))
                    {
                        // If original file exists, delete it
                        if (File.Exists(StateLogFilePath))
                        {
                            File.Delete(StateLogFilePath);
                        }
                        // Rename temp file to proper name
                        File.Move(tempFilePath, StateLogFilePath);
                    }

                    // Notify TCPServer about the update
                    // _tcpServer?.HandleStateFileUpdate();
                }
                catch (Exception ex)
                {
                    LogError($"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz} | LOGGER_FAILURE in UpdateAllJobsState: {ex.Message}{Environment.NewLine}");
                    // Log error but don't rethrow to avoid disrupting app flow
                }
            }
        }

        /// <summary>
        /// Loads backup job configurations from the state.json file.
        /// </summary>
        /// <returns>A list of backupJob objects.</returns>
        public List<backupJob>? LoadJobsState()
        {
            var loadedJobs = new List<backupJob>();
            lock (logLock)
            {
                if (!File.Exists(StateLogFilePath))
                {
                    return []; // Return empty list if state file doesn\'t exist
                }
                // Read the JSON content from the state file
                string jsonContent = File.ReadAllText(StateLogFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return []; // Return empty list if state file is empty
                }

                // Deserialize the JSON content to a list of job states
                var jobStates = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

                if (jobStates == null) return loadedJobs; // Return empty list if deserialization fails

                foreach (var jobStateElement in jobStates)
                {
                    try
                    {
                        string name = jobStateElement.GetProperty("Name").GetString() ?? "";
                        string sourceDir = jobStateElement.GetProperty("SourceDirectory").GetString() ?? "";
                        string targetDir = jobStateElement.GetProperty("TargetDirectory").GetString() ?? "";
                        string typeStr = jobStateElement.GetProperty("Type").GetString() ?? JobType.Full.ToString();
                        JobType type = Enum.Parse<JobType>(typeStr, true);
                        string stateStr = jobStateElement.GetProperty("State").GetString() ?? JobStates.Stopped.ToString();

                        // parse the state, only accept stopped, paused and failed states
                        if (!Enum.TryParse(stateStr, true, out JobStates state) || state != JobStates.Failed)
                        {
                            state = JobStates.Stopped; // Default to Stopped if parsing fails or state is not valid
                        }

                        int totalFilesToCopy = jobStateElement.GetProperty("TotalFilesToCopy").GetInt32();
                        int numberFilesLeftToDo = jobStateElement.GetProperty("NumberFilesLeftToDo").GetInt32();
                        int totalFilesCopied = totalFilesToCopy - numberFilesLeftToDo;

                        // Create the job instance and set its properties
                        var job = new backupJob(name, sourceDir, targetDir, type, this)
                        {
                            State = state,
                            TotalFilesToCopy = totalFilesToCopy,
                            NumberFilesLeftToDo = numberFilesLeftToDo,
                            TotalFilesCopied = totalFilesCopied,
                            TotalSizeToCopy = jobStateElement.GetProperty("TotalFilesSize").GetUInt64(),
                            Progress = (float)jobStateElement.GetProperty("Progress").GetDouble(),
                            ErrorMessage = jobStateElement.GetProperty("ErrorMessage").GetString() ?? ""
                        };
                        // job.UpdateProgress(); // Removed: Progress is now loaded directly

                        loadedJobs.Add(job);
                    }
                    catch (Exception ex)
                    {
                        LogError($"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz} | LOGGER_FAILURE in LoadJobsState: {ex.Message} | Job State Element: {jobStateElement}{Environment.NewLine}");
                        // Continue loading other jobs
                    }
                }
            }
            return loadedJobs;
        }
    }
}