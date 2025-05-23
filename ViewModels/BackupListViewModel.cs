using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using better_saving.Models;

namespace better_saving.ViewModels
{
    public class BackupListViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private bool _isAlphabeticalSort = false;
        private ObservableCollection<backupJob> _jobs = new ObservableCollection<backupJob>();
        private readonly Logger _logger;
        private string? _errorMessage;

        public ObservableCollection<backupJob> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand CreateJobCommand { get; }
        public ICommand StartAllJobsCommand { get; }
        public ICommand FilterJobsCommand { get; }
        public ICommand ShowJobDetailsCommand { get; }

        public BackupListViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _logger = new Logger();
            _logger.SetJobProvider(() => Jobs); // Provide the Jobs collection to the logger
            LoadJobsFromStateLog(); // Renamed method call

            CreateJobCommand = new RelayCommand(_ => CreateNewJob());
            StartAllJobsCommand = new RelayCommand(_ => StartAllJobs(), _ => CanStartAllJobs());
            FilterJobsCommand = new RelayCommand(_ => SortJobs());
            ShowJobDetailsCommand = new RelayCommand(param => ShowJobDetails(param as backupJob));
        }

        public Logger GetLogger() // Added to allow access to the logger instance
        {
            return _logger;
        }        private void LoadJobsFromStateLog() // Renamed method
        {
            var loadedJobs = _logger.LoadJobsState();
            if (loadedJobs != null && loadedJobs.Count > 0)
            {
                Jobs = new ObservableCollection<backupJob>(loadedJobs);
                // No additional logger setup needed as the logger is already passed during job creation in LoadJobsState()
            }
            // If no jobs are loaded, we keep the empty Jobs collection as is without updating state.json
        }        public void AddJob(backupJob job)
        {
            Jobs.Add(job);
            _logger.UpdateAllJobsState(); // Logger now gets jobs via provider
            OnPropertyChanged(nameof(Jobs));
        }

        public void RemoveJob(backupJob jobToRemove)
        {
            if (Jobs.Contains(jobToRemove))
            {
                Jobs.Remove(jobToRemove);
                // Only update state.json if there are still jobs left
                if (Jobs.Count > 0)
                {
                    _logger.UpdateAllJobsState();
                }
                OnPropertyChanged(nameof(Jobs));
            }
        }
        /// <summary>
        /// Shows the backup creation view
        /// </summary>

        private void CreateNewJob()
        {
            _mainViewModel.ShowCreateJobViewInternal();
        }
        /// <summary>
        /// Starts all backup jobs
        /// </summary>

        private bool CanStartAllJobs()
        {
            // Check if any job is not in the Working state and if no blocked software is running
            return Jobs.Any(j => j.State != JobStates.Working) && !_mainViewModel.IsSoftwareRunning();
        }

        private void StartAllJobs()
        {
            if (_mainViewModel.IsSoftwareRunning())
            {
                // If any blocked software is running, set the error message and log it
                ErrorMessage = $"Cannot start jobs: {_mainViewModel.GetRunningBlockedSoftware()} is running.";
                _logger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "StartAllJobs", ErrorMessage, 0, 0);
                return;
            }

            // For each job in Jobs collection, start the backup process asynchronously
            foreach (var job in Jobs)
            {
                if (job.State != JobStates.Working)
                {
                    // Start each job in a separate task to allow them to run in parallel
                    Task.Run(async () =>
                    {
                        // Create a CancellationTokenSource for each job
                        var cts = new CancellationTokenSource();
                        try
                        {
                            // Pass only the CancellationToken as expected by BackupJob.ExecuteAsync
                            await job.ExecuteAsync(cts.Token);
                        }
                        catch (Exception ex)
                        {
                            job.ErrorMessage = $"Error starting job: {ex.Message}";
                            job.State = JobStates.Failed;
                            OnPropertyChanged(nameof(Jobs)); // update UI on error
                        }
                    });
                }
            }

            _logger.UpdateAllJobsState(); // Logger now gets jobs via provider
            // Notify UI to refresh job list after starting all jobs
            OnPropertyChanged(nameof(Jobs));
        }        /// <summary>
                 /// Filters backup jobs based on certain criteria
                 /// </summary>

        private void SortJobs()
        {
            _isAlphabeticalSort = !_isAlphabeticalSort; // Toggle sort mode

            var tempList = Jobs.ToList(); // Create a temporary list for sorting

            if (_isAlphabeticalSort)
            {
                // Sort alphabetically by name
                tempList = [.. tempList.OrderBy(j => j.Name)];
            }
            else
            {
                // Sort by state: Finished > Working > Idle > Failed
                tempList = [.. tempList.OrderBy(j => j.State switch
                {
                    JobStates.Finished => 0,
                    JobStates.Working => 1,
                    JobStates.Failed => 2,
                    JobStates.Stopped => 3,
                    JobStates.Idle => 4,
                    _ => 5 // Default case for any other states
                }).ThenBy(j => j.Name)];
            }

            // Clear the original collection and add sorted items
            Jobs = new ObservableCollection<backupJob>(tempList);
            // No need to call _logger.UpdateAllJobsState here as filtering doesn't change persistent state.
        }

        private void ShowJobDetails(backupJob? job)
        {
            if (job != null)
            {
                _mainViewModel.ShowJobStatus(job);
            }
        }
         // Method to be called by BackupCreationViewModel
        // public void AddJobToList(backupJob newJob)
        // {
        //     Jobs.Add(newJob);
        //     _logger.UpdateAllJobsState(Jobs.ToList()); // Persist the new job
        //     OnPropertyChanged(nameof(Jobs)); // Notify the UI that the collection has changed
        // }

        // Consider adding a method to remove jobs as well, if needed
        // public void RemoveJob(backupJob jobToRemove)
        // {
        //     Jobs.Remove(jobToRemove);
        //     _logger.UpdateAllJobsState(Jobs.ToList()); // Persist the change
        //     OnPropertyChanged(nameof(Jobs));
        // }
    }
}