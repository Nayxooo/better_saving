using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input; // Required for ICommand
using EasySave.Models;

namespace EasySave.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Logger _applicationLogger;
        private Dictionary<string, CancellationTokenSource> _runningJobs = new Dictionary<string, CancellationTokenSource>();
        private ObservableCollection<BackupJobViewModel> _backupJobs = new ObservableCollection<BackupJobViewModel>();
        public ObservableCollection<BackupJobViewModel> BackupJobs
        {
            get => _backupJobs;
            set
            {
                _backupJobs = value;
                OnPropertyChanged();
            }
        }

        private BackupJobViewModel? _selectedJob;
        public BackupJobViewModel? SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged();
                (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // --- Commands ---
        public ICommand CreateJobCommand { get; }
        public ICommand StartJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand StartAllJobsCommand { get; }

        public MainViewModel(string logsDirectory)
        {
            _applicationLogger = new Logger(logsDirectory);
            LoadBackupJobs();

            // Initialize commands
            CreateJobCommand = new RelayCommand(
                param => CreateBackupJob("Default Name", "C:\\Source", "C:\\Target", JobType.Full), // Placeholder parameters
                param => true);
            StartJobCommand = new RelayCommand(
                async param => { if (SelectedJob?.Name != null) await StartJobAsync(SelectedJob.Name); },
                param => SelectedJob != null && !_runningJobs.ContainsKey(SelectedJob.Name) && SelectedJob.State != JobState.Working && SelectedJob.State != JobState.Finished);
            StopJobCommand = new RelayCommand(
                param => { if (SelectedJob?.Name != null) StopJob(SelectedJob.Name); },
                param => SelectedJob != null && _runningJobs.ContainsKey(SelectedJob.Name) && SelectedJob.State == JobState.Working);
            DeleteJobCommand = new RelayCommand(
                param => { if (SelectedJob?.Name != null) DeleteJob(SelectedJob.Name); },
                param => SelectedJob != null && !_runningJobs.ContainsKey(SelectedJob.Name));
            StartAllJobsCommand = new RelayCommand(ExecuteStartAllJobs, CanExecuteStartAllJobs);
        }

        private void LoadBackupJobs()
        {
            var jobModels = _applicationLogger.LoadJobsFromStateFile();
            BackupJobs.Clear(); // Clear existing jobs before loading
            foreach (var jobModel in jobModels)
            {
                var jobViewModel = new BackupJobViewModel(jobModel);
                _backupJobs.Add(jobViewModel);
            }
            OnPropertyChanged(nameof(BackupJobs)); // Notify that the collection has changed
        }

        public void CreateBackupJob(string name, string sourceDir, string targetDir, JobType type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "CreateJob", "Error: Job name cannot be empty.", 0, 0);
                return;
            }

            if (_backupJobs.Any(j => j.Name == name))
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "CreateJob", $"Error: Job with name '{name}' already exists.", 0, 0);
                return;
            }

            if (!Directory.Exists(sourceDir))
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "CreateJob", $"Error: Source directory '{sourceDir}' not found for job '{name}'.", 0, 0);
                return;
            }

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
            }
            catch (Exception ex)
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "CreateJob", $"Error: Failed to create target directory '{targetDir}' for job '{name}'. Exception: {ex.Message}", 0, 0);
                return;
            }

            var newJobModel = new backupJob(name, sourceDir, targetDir, type, _applicationLogger);
            var newJobViewModel = new BackupJobViewModel(newJobModel);
            _backupJobs.Add(newJobViewModel);
            _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList());
            OnPropertyChanged(nameof(BackupJobs));
            _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), name, "SystemOperation", "JobCreated", 0, 0);
        }

        public async Task StartJobAsync(string jobName)
        {
            var jobViewModel = _backupJobs.FirstOrDefault(j => j.Name == jobName);
            if (jobViewModel == null)
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", $"Error: Job '{jobName}' not found for starting.", 0, 0);
                return;
            }

            if (_runningJobs.ContainsKey(jobName))
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", $"Info: Job '{jobName}' is already running.", 0, 0);
                return;
            }

            var cts = new CancellationTokenSource();
            _runningJobs[jobName] = cts;

            try
            {
                jobViewModel.State = JobState.Working;
                _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList());
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobStarting", 0, 0);

                await jobViewModel.Model.ExecuteAsync(
                    _applicationLogger,
                    progress =>
                    {
                        jobViewModel.Progress = (byte)progress;
                        _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList()); // Update state on progress
                    },
                    cts.Token);

                if (cts.Token.IsCancellationRequested)
                {
                    jobViewModel.State = JobState.Stopped;
                    jobViewModel.ErrorMessage = "Job was cancelled by the user.";
                    _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobCancelledByUser", 0, 0);
                }
                else
                {
                    if (jobViewModel.Model.GetState() != JobState.Failed)
                    {
                        jobViewModel.State = JobState.Finished;
                        _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobFinishedSuccessfully", (ulong)jobViewModel.Model.GetTotalFilesSize(), 0);
                    }
                    else
                    {
                        _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", $"JobFailed: {jobViewModel.Model.GetErrorMessage()}", 0, 0);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                jobViewModel.State = JobState.Stopped;
                jobViewModel.ErrorMessage = "Job was cancelled.";
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobCancelledOperation", 0, 0);
            }
            catch (Exception ex)
            {
                jobViewModel.State = JobState.Failed;
                jobViewModel.ErrorMessage = $"Job '{jobName}' failed: {ex.Message}";
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", $"JobFailedException: {ex.Message}", 0, 0);
            }
            finally
            {
                _runningJobs.Remove(jobName);
                _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList());
                (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public void StopJob(string jobName)
        {
            if (_runningJobs.TryGetValue(jobName, out var cts))
            {
                cts.Cancel();
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "StopRequestSent", 0, 0);
            }
            else
            {
                var jobViewModel = _backupJobs.FirstOrDefault(j => j.Name == jobName);
                if (jobViewModel != null && jobViewModel.State == JobState.Working)
                {
                    jobViewModel.State = JobState.Stopped;
                    jobViewModel.ErrorMessage = "Job stopped (was in working state without active token).";
                    _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList());
                    _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobMarkedAsStopped", 0, 0);
                }
                else
                {
                    _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobNotRunningOrCannotBeStopped", 0, 0);
                }
            }
            (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public void DeleteJob(string jobName)
        {
            var jobViewModel = _backupJobs.FirstOrDefault(j => j.Name == jobName);
            if (jobViewModel != null)
            {
                if (_runningJobs.ContainsKey(jobName))
                {
                    _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "Error: CannotDeleteRunningJob", 0, 0);
                    return;
                }
                _backupJobs.Remove(jobViewModel);
                _applicationLogger.UpdateAllJobsState(BackupJobs.Select(vm => vm.Model).ToList());
                OnPropertyChanged(nameof(BackupJobs));
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "JobDeleted", 0, 0);
            }
            else
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), jobName, "SystemOperation", "Error: JobNotFoundForDeletion", 0, 0);
            }
            (DeleteJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // --- StartAllJobs Command Implementation ---
        private void ExecuteStartAllJobs(object? parameter)
        {
            _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "StartAllJobs", "Attempting to start all eligible jobs.", 0, 0);
            var jobsToStart = BackupJobs.Where(job => !_runningJobs.ContainsKey(job.Name) && job.State != JobState.Working && job.State != JobState.Finished).ToList();
            if (!jobsToStart.Any())
            {
                _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "StartAllJobs", "No jobs eligible to start.", 0, 0);
                return;
            }

            foreach (var jobViewModel in jobsToStart)
            {
                #pragma warning disable CS4014
                StartJobAsync(jobViewModel.Name);
                #pragma warning restore CS4014
            }
            _applicationLogger.LogBackupDetails(DateTime.Now.ToString("o"), "System", "StartAllJobs", $"Queued {jobsToStart.Count} job(s) to start.", 0, 0);
            (StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private bool CanExecuteStartAllJobs(object? parameter)
        {
            return BackupJobs.Any(job => !_runningJobs.ContainsKey(job.Name) && job.State != JobState.Working && job.State != JobState.Finished);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class BackupJobViewModelExtensions
    {
        public static backupJob? GetModel(this BackupJobViewModel viewModel)
        {
            if (viewModel == null) return null;

            return viewModel.Model;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
