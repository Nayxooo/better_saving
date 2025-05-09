using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;

// Enum for job types
public enum JobType
{
    Full, // Full backup
    Diff // Differential backup
}

public enum jobState
{
    Working, // Job is currently running
    Finished, // Job has finished successfully
    Stopped, // Job has been stopped
    Failed, // Job has failed
    Idle // Job has been created but not started
}

public class Controller
{
    private static List<backupJob> backupJobsList = []; // list of all jobs
    private static Logger? applicationLogger; // Logger instance, not nullable
    
    // Dictionary to keep track of running jobs and their cancellation tokens
    private static Dictionary<string, CancellationTokenSource> runningJobs = [];

    // Constructor to initialize the logger
    public Controller(string logsDirectory)
    {
        applicationLogger = new Logger(logsDirectory);
        // Load existing backup jobs from state.json
        LoadBackupJobsFromState();

        // Pass logger to ConsoleInterface
        ConsoleInterface.Initialize(applicationLogger);

        // Start the console interface
        ConsoleInterface.Start();
    }

    // Getter method to access the jobs list
    public static List<backupJob> RetrieveBackupJobs()
    {
        return backupJobsList;
    }

    public static void CreateBackupJob(string name, string sourceDir, string targetDir, JobType type)
    {
        try
        {
            // Check if a job with the same name already exists
            if (backupJobsList.Any(j => j.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new InvalidOperationException($"A job with the name '{name}' already exists.");
            }
            
            // Check if source directory exists
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Source directory '{sourceDir}' does not exist.");
            }
            
            // Create target directory if it doesn't exist
            if (!Directory.Exists(targetDir))
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    throw new IOException($"Failed to create target directory: {ex.Message}", ex);
                }
            }


            // Create the backup job
            backupJob job = new(name, sourceDir, targetDir, type, 5, applicationLogger);
            backupJobsList.Add(job);
            // trigger the state update
            applicationLogger.UpdateAllJobsState();
        }
        catch (Exception)
        {
            // Re-throw the exception to be handled by the caller
            throw;
        }
    }

    // Asynchronous method to start a job
    public static async Task StartJobAsync(string name)
    {
        // Find the job by name
        backupJob? job = backupJobsList.Find(j => j.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        if (job != null)
        {
            // Check if job is already running
            if (runningJobs.ContainsKey(name))
            {
                Console.WriteLine($"Job '{name}' is already running.");
                return;
            }
            
            // Create cancellation token source for this job
            var cancellationTokenSource = new CancellationTokenSource();
            runningJobs[name] = cancellationTokenSource;
            
            try
            {
                // Run the job asynchronously
                await Task.Run(async () => {
                    try 
                    {
                        await job.ExecuteAsync(cancellationTokenSource.Token);
                    }
                    finally 
                    {
                        // Remove from running jobs when complete regardless of success or failure
                        if (runningJobs.ContainsKey(name))
                        {
                            runningJobs.Remove(name);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running job '{name}': {ex.Message}");
                // Ensure job is removed from running jobs on error
                if (runningJobs.ContainsKey(name))
                {
                    runningJobs.Remove(name);
                }
            }
        }
        else
        {
            Console.WriteLine($"Job '{name}' not found.");
        }
    }
    
    // For backward compatibility - starts job asynchronously but doesn't wait for completion
    public static void StartJob(string name)
    {
        // Start job without awaiting its completion
        _ = StartJobAsync(name);
    }
    
    // Stop a running job
    public static void StopJob(string name)
    {
        if (runningJobs.TryGetValue(name, out var cancellationTokenSource))
        {
            // Signal cancellation to the job
            cancellationTokenSource.Cancel();
            Console.WriteLine($"Job '{name}' was signaled to stop.");
        }
        else
        {
            Console.WriteLine($"Job '{name}' is not currently running.");
        }
    }

    // Check if a job is currently running
    public static bool IsJobRunning(string name)
    {
        return runningJobs.ContainsKey(name);
    }

    public static async Task StartAllJobsAsync()
    {
        var tasks = new List<Task>();
        foreach (var job in backupJobsList)
        {
            tasks.Add(StartJobAsync(job.Name));
        }
        
        // Wait for all jobs to complete
        await Task.WhenAll(tasks);
    }
    
    // For backward compatibility
    public static void StartAllJobs()
    {
        // Start all jobs without awaiting completion
        _ = StartAllJobsAsync();
    }
    
    // Delete a backup job
    public static bool DeleteBackupJob(string name)
    {
        // Check if the job is currently running
        if (IsJobRunning(name))
        {
            // Cannot delete a job that is running
            return false;
        }
        
        // Find the job by name
        backupJob? job = backupJobsList.Find(j => j.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        if (job != null)
        {
            // Remove the job from the list
            backupJobsList.Remove(job);
            // update the state.json file
            applicationLogger.UpdateAllJobsState();          
            return true;
        }
        else
        {
            return false;
        }
    }

    // Method to load backup jobs from the state.json file
    public static void LoadBackupJobsFromState()
    {
        string stateFilePath = Path.Combine(applicationLogger.GetLogDirectory(), "state.json");
        
        try
        {
            if (File.Exists(stateFilePath))
            {
                string jsonContent = File.ReadAllText(stateFilePath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Console.WriteLine("State file is empty.");
                    return;
                }
                
                JsonArray? jobsArray = JsonNode.Parse(jsonContent) as JsonArray;
                
                if (jobsArray == null)
                {
                    Console.WriteLine("Failed to parse state file as JSON array.");
                    return;
                }
                
                foreach (JsonNode? jobNode in jobsArray)
                {
                    if (jobNode == null) continue;
                    
                    try
                    {
                        string? name = jobNode["Name"]?.GetValue<string>();
                        string? sourceDir = jobNode["SourceDirectory"]?.GetValue<string>();
                        string? targetDir = jobNode["TargetDirectory"]?.GetValue<string>();
                        string? jobTypeStr = jobNode["Type"]?.GetValue<string>();
                        
                        if (name == null || sourceDir == null || targetDir == null || jobTypeStr == null)
                        {
                            Console.WriteLine("Job in state file is missing required properties.");
                            continue;
                        }
                        
                        JobType jobType;
                        if (!Enum.TryParse(jobTypeStr, true, out jobType))
                        {
                            Console.WriteLine($"Invalid job type: {jobTypeStr}");
                            continue;
                        }
                        
                        
                        // Create the backup job
                        try
                        {
                            CreateBackupJob(name, sourceDir, targetDir, jobType);
                            Console.WriteLine($"Loaded backup job: {name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating job '{name}': {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing job from state file: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("State file does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading backup jobs from state file: {ex.Message}");
        }
    }
}
