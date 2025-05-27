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
        private ObservableCollection<backupJob> _jobs = new ObservableCollection<backupJob>();
        private readonly Logger _logger;
        private backupJob? _selectedBackupJob;

        public ObservableCollection<backupJob> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        public backupJob? SelectedBackupJob
        {
            get => _selectedBackupJob;
            set
            {
                if (SetProperty(ref _selectedBackupJob, value) && value != null)
                {
                    _mainViewModel.ShowJobStatus(value);
                }
            }
        }

        public BackupListViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _logger = new Logger();
            _logger.SetJobProvider(() => Jobs);
            LoadJobsFromStateLog();
        }

        public Logger GetLogger()
        {
            return _logger;
        }

        private void LoadJobsFromStateLog()
        {
            var loadedJobs = _logger.LoadJobsState();
            if (loadedJobs != null && loadedJobs.Count > 0)
            {
                Jobs = new ObservableCollection<backupJob>(loadedJobs);
            }
        }

        public void AddJob(backupJob job)
        {
            Jobs.Add(job);
            _logger.UpdateAllJobsState();
            OnPropertyChanged(nameof(Jobs));
        }

        public void RemoveJob(backupJob jobToRemove)
        {
            if (Jobs.Contains(jobToRemove))
            {
                Jobs.Remove(jobToRemove);
                if (Jobs.Count > 0)
                {
                    _logger.UpdateAllJobsState();
                }
                OnPropertyChanged(nameof(Jobs));
            }
        }

        public void UpdateBackupJob(backupJob updatedJob)
        {
            var existingJob = Jobs.FirstOrDefault(j => j.Name == updatedJob.Name);
            if (existingJob != null)
            {
                var index = Jobs.IndexOf(existingJob);
                Jobs[index] = updatedJob;
                _logger.UpdateAllJobsState();
                OnPropertyChanged(nameof(Jobs));
            }
        }

        public void RemoveBackupJob(string jobName)
        {
            var jobToRemove = Jobs.FirstOrDefault(j => j.Name == jobName);
            if (jobToRemove != null)
            {
                RemoveJob(jobToRemove);
            }
        }
    }
}