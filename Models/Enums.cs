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
        Stopped, // Job has been stopped
        Failed, // Job has failed
        Idle // Job has been created but not started
    }

        public enum RemoteCommands
    {
        PING,
        START_JOB, // Start a job
        PAUSE_JOB, // Stop a job
        STOP_JOB, // Pause a job
        UNKNOWN // Unknown command
    }
}
