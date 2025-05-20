using better_saving.Models; 
using System.ComponentModel; 
using System.Windows.Input; // Added for ICommand
using System.Threading; // Added for CancellationTokenSource
using System.Threading.Tasks; // Added for Task
namespace better_saving.ViewModels
{
    public class BackupStatusViewModel : ViewModelBase
    {
        private backupJob _selectedJob;
        private readonly MainViewModel? _mainViewModel;
        private CancellationTokenSource? _jobExecutionCts; // Added to manage job execution cancellation

        public backupJob SelectedJob
        {
            get => _selectedJob;
            // Setter might not be strictly needed if the VM is recreated each time a job is selected
            set => SetProperty(ref _selectedJob, value);
        }

        // Constructor that accepts MainViewModel for operations like deletion
        public BackupStatusViewModel(backupJob selectedJob, MainViewModel? mainViewModel = null)
        {
            _selectedJob = selectedJob;
            _mainViewModel = mainViewModel;
            _selectedJob.PropertyChanged += SelectedJob_PropertyChanged;

            PauseResumeJobCommand = new RelayCommand(ExecutePauseResumeJob, CanExecutePauseResumeJob);
            DeleteJobCommand = new RelayCommand(ExecuteDeleteJob, CanExecuteDeleteJob);
        }

        private void SelectedJob_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // When a property of the SelectedJob changes,
            // raise a PropertyChanged event for the corresponding property in this ViewModel.
            // This will ensure the UI updates.
            switch (e.PropertyName)
            {
                case nameof(backupJob.Name):
                    OnPropertyChanged(nameof(JobName));
                    break;
                case nameof(backupJob.SourceDirectory):
                    OnPropertyChanged(nameof(SourceDirectory));
                    break;
                case nameof(backupJob.TargetDirectory):
                    OnPropertyChanged(nameof(TargetDirectory));
                    break;
                case nameof(backupJob.Type):
                    OnPropertyChanged(nameof(JobType));
                    break;
                case nameof(backupJob.State):
                    OnPropertyChanged(nameof(JobState));
                    break;
                case nameof(backupJob.Progress):
                    OnPropertyChanged(nameof(JobProgress));
                    break;
                case nameof(backupJob.TotalFilesToCopy):
                    OnPropertyChanged(nameof(TotalFilesToCopy));
                    break;
                case nameof(backupJob.TotalFilesCopied):
                    OnPropertyChanged(nameof(TotalFilesCopied));
                    break;
                case nameof(backupJob.TotalSizeToCopy):
                    OnPropertyChanged(nameof(TotalSizeToCopy));
                    break;
                case nameof(backupJob.TotalSizeCopied):
                    OnPropertyChanged(nameof(TotalSizeCopied));
                    break;                case nameof(backupJob.ErrorMessage):
                    OnPropertyChanged(nameof(ErrorMessage));
                    break;
                case nameof(backupJob.IsPausing):
                    OnPropertyChanged(nameof(IsJobPausing));
                    break;
                // Add other properties if needed
            }
        }
        // Expose properties of SelectedJob that BackupStatusView will bind to

        public string JobName => SelectedJob.Name;
        public string SourceDirectory => SelectedJob.SourceDirectory;
        public string TargetDirectory => SelectedJob.TargetDirectory;
        public string JobType => SelectedJob.Type.ToString();        public string JobState => SelectedJob.State.ToString();
        public bool IsJobPausing => SelectedJob.IsPausing;
        public byte JobProgress => SelectedJob.Progress;
        public long TotalFilesToCopy => SelectedJob.TotalFilesToCopy;
        public long TotalFilesCopied => SelectedJob.TotalFilesCopied;
        public ulong TotalSizeToCopy => SelectedJob.TotalSizeToCopy;
        public long TotalSizeCopied => SelectedJob.TotalSizeCopied;
        public string? ErrorMessage => SelectedJob.ErrorMessage;

        public ICommand PauseResumeJobCommand { get; }
        public ICommand DeleteJobCommand { get; }

        //removed CanExecutePauseResumeJob method

        private bool CanExecutePauseResumeJob(object? parameter)
        {
            return (SelectedJob.State == JobStates.Working ||
                    SelectedJob.State == JobStates.Idle ||
                    SelectedJob.State == JobStates.Stopped ||
                    SelectedJob.State == JobStates.Failed ||
                    SelectedJob.State == JobStates.Finished) &&
                    (_mainViewModel == null || !_mainViewModel.IsSoftwareRunning());
        }


        private async void ExecutePauseResumeJob(object? parameter)
        {
            if (SelectedJob == null) return;

            var logger = _mainViewModel?.ListVM.GetLogger();

            // Logger is essential for starting or resuming a job.
            // If the job is already working, we are pausing it, so logger isn't strictly needed for the pause action itself.
            if (logger == null && SelectedJob.State != JobStates.Working)
            {
                SelectedJob.ErrorMessage = "Logger not available. Cannot start/resume job.";
                OnPropertyChanged(nameof(ErrorMessage));
                return;
            }

            if (_mainViewModel?.IsSoftwareRunning() == true)
            {
                SelectedJob.ErrorMessage = $"Cannot start/resume job: {_mainViewModel.GetRunningBlockedSoftware()} is running.";
                _mainViewModel.ListVM.GetLogger().LogBackupDetails(DateTime.Now.ToString("o"), SelectedJob.Name, "SystemOperation", SelectedJob.ErrorMessage, 0, 0);
                OnPropertyChanged(nameof(ErrorMessage));
                return;
            }

            try
            {                if (SelectedJob.State == JobStates.Working)
                {
                    // PAUSE action
                    if (_jobExecutionCts != null && !_jobExecutionCts.IsCancellationRequested)
                    {
                        // Set IsPausing flag to true
                        SelectedJob.IsPausing = true;
                        
                        _jobExecutionCts.Cancel();
                        // ExecuteAsync in BackupJob should handle the OperationCanceledException
                        // and set the job's state to Stopped.
                    }
                }
                else if (SelectedJob.State == JobStates.Idle ||
                         SelectedJob.State == JobStates.Stopped ||
                         SelectedJob.State == JobStates.Failed ||
                         SelectedJob.State == JobStates.Finished) // Allow re-running/resuming from these states
                {
                    // START or RESUME action
                    _jobExecutionCts?.Dispose(); // Dispose any existing CTS
                    _jobExecutionCts = new CancellationTokenSource();

                    // Execute the job on a background thread    
                    await Task.Run(async () =>
                    {
                        try
                        {
                            if (logger == null) // Should not happen if the initial check passed, but as a safeguard
                            {
                                SelectedJob.State = JobStates.Failed;
                                SelectedJob.ErrorMessage = "Critical error: Logger became null before job execution.";
                                return;
                            }
                            await SelectedJob.ExecuteAsync(_jobExecutionCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // This is expected if the job is paused/stopped.
                            // The state should be set to Stopped within ExecuteAsync.
                            // If not already Stopped, explicitly set it.
                            if (SelectedJob.State != JobStates.Stopped)
                            {
                                SelectedJob.State = JobStates.Stopped;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (SelectedJob.State != JobStates.Failed && SelectedJob.State != JobStates.Stopped)
                            {
                                SelectedJob.State = JobStates.Failed;
                            }
                            SelectedJob.ErrorMessage = $"Job execution encountered an error: {ex.Message}";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SelectedJob.ErrorMessage = $"Command execution error: {ex.Message}";
                if (SelectedJob.State != JobStates.Failed)
                {
                    SelectedJob.State = JobStates.Failed;
                }
            }
        }

        private bool CanExecuteDeleteJob(object? parameter)
        {
            return _mainViewModel != null && _mainViewModel.ListVM != null;
        }

        private void ExecuteDeleteJob(object? parameter)
        {
            if (_selectedJob == null) return;

            if (_mainViewModel?.ListVM.Jobs.Contains(_selectedJob) ?? false)
            {
                // Stop the job if it's running
                if (_selectedJob.State == JobStates.Working)
                {
                    if (_jobExecutionCts != null && !_jobExecutionCts.IsCancellationRequested)
                    {
                        _jobExecutionCts.Cancel(); // Request cancellation
                        // ExecuteAsync should catch OperationCanceledException and set State to Stopped.
                    }
                }

                _mainViewModel.ListVM.RemoveJob(_selectedJob);
                _mainViewModel.CurrentView = null; // Clear the detail view, returning to the default (job list)

                // Clean up resources associated with this job in this ViewModel instance
                UnsubscribeFromJobEvents();
            }
        }

        // It's good practice to unsubscribe from events when the ViewModel is no longer needed
        // to prevent memory leaks, though in this specific navigation pattern,
        // the ViewModel instance might be short-lived.
        // If this ViewModel were to be kept alive longer, implement IDisposable.
        public void UnsubscribeFromJobEvents()
        {
            if (_selectedJob != null)
            {
                _selectedJob.PropertyChanged -= SelectedJob_PropertyChanged;
            }
            _jobExecutionCts?.Cancel(); // Cancel any ongoing execution
            _jobExecutionCts?.Dispose(); // Dispose the CancellationTokenSource
            _jobExecutionCts = null;
        }
    }
}