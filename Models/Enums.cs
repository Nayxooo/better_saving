namespace better_saving.Models
{
    // Enum for job types
    public enum JobType
    {
        Full, // Full backup
        Diff // Differential backup
    }

    public enum JobStates
    {
        Working, // Job is currently running
        Finished, // Job has finished successfully
        Failed, // Job has failed
        Stopped, // Job has been created but not started
        Paused // Job is temporarily paused
    }
}
