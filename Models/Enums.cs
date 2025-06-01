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

    public enum RemoteCommands
    {
        PING,
        START_JOB, // Start a job
        RESUME_JOB, // Resume a paused job
        PAUSE_JOB, // Stop a job
        STOP_JOB, // Pause a job
        GET_JOBS, // Get the state.json file (without sensitive data)
        UNKNOWN // Unknown command
    }

    // Enum for BackupJob Information Message Keys
    public enum BackupJobInfoMessageKeys
    {
        Scanning,
        JobPausedOrStoppedDuringScan
    }

    // Enum for BackupJob Error Message Keys
    public enum BackupJobErrorMessageKeys
    {
        SourceDirectoryDoesNotExist,
        TargetDirectoryDoesNotExist,
        ErrorScanningSourceDirectory,
        CryptoSoftInternalError,
        CryptoSoftDownloadFailed,
        CryptoSoftNotFoundAfterDownload,
        CryptoSoftProcessStartFailed,
        EncryptionException,
        BlockedSoftware,
        ErrorCreatingTargetDirectory,
        FilesSkippedDueToSizeLimits,
        ErrorCopyingFileToTarget,
        ErrorEncryptingFileWithCode,
        ErrorCopyingFileWithException,
        ErrorStartingJob,
        CannotPauseJobInState,
        CannotResumeJobInState,
        ErrorResumingJob,
        GenericError // General error message
    }
}
