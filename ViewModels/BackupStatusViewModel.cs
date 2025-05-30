using better_saving.Models;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace better_saving.ViewModels
{
    public class BackupStatusViewModel : ViewModelBase
    {
        private readonly backupJob _job;
        private readonly MainViewModel _mainViewModel;
        private string _status = "En attente";
        private int _progress;
        private string _currentFile = "";
        private long _filesRemaining;
        private long _filesTotal;
        private long _bytesRemaining;
        private long _bytesTotal;
        private TimeSpan _estimatedTimeRemaining;

        public string JobName => _job.Name;
        public string SourceDirectory => _job.SourceDirectory;
        public string TargetDirectory => _job.TargetDirectory;
        public string JobType => _job.Type.ToString();
        public string JobState => _job.State.ToString();
        public byte JobProgress => _job.Progress;
        public long TotalFilesToCopy => _job.TotalFilesToCopy;
        public long TotalFilesCopied => _job.TotalFilesCopied;
        public ulong TotalSizeToCopy => _job.TotalSizeToCopy;
        public long TotalSizeCopied => _job.TotalSizeCopied;
        public string? ErrorMessage => _job.ErrorMessage;

        public ICommand StartJobCommand { get; }
        public ICommand PauseJobCommand { get; }
        public ICommand StopJobCommand { get; }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string CurrentFile
        {
            get => _currentFile;
            set => SetProperty(ref _currentFile, value);
        }

        public long FilesRemaining
        {
            get => _filesRemaining;
            set => SetProperty(ref _filesRemaining, value);
        }

        public long FilesTotal
        {
            get => _filesTotal;
            set => SetProperty(ref _filesTotal, value);
        }

        public long BytesRemaining
        {
            get => _bytesRemaining;
            set => SetProperty(ref _bytesRemaining, value);
        }

        public long BytesTotal
        {
            get => _bytesTotal;
            set => SetProperty(ref _bytesTotal, value);
        }

        public TimeSpan EstimatedTimeRemaining
        {
            get => _estimatedTimeRemaining;
            set => SetProperty(ref _estimatedTimeRemaining, value);
        }

        public BackupStatusViewModel(backupJob job, MainViewModel mainViewModel)
        {
            _job = job;
            _mainViewModel = mainViewModel;
            
            StartJobCommand = new RelayCommand(_ => StartJob(), _ => CanStartJob());
            PauseJobCommand = new RelayCommand(_ => PauseJob(), _ => CanPauseJob());
            StopJobCommand = new RelayCommand(_ => StopJob(), _ => CanStopJob());
            
            SubscribeToJobEvents();
        }

        private bool CanStartJob()
        {
            return _job.State == JobStates.Idle || _job.State == JobStates.Stopped;
        }

        private bool CanPauseJob()
        {
            return _job.State == JobStates.Working;
        }

        private bool CanStopJob()
        {
            return _job.State == JobStates.Working || _job.State == JobStates.Idle;
        }

        private async void StartJob()
        {
            await _mainViewModel.ListVM.StartJob(_job.Name);
        }

        private async void PauseJob()
        {
            await _mainViewModel.ListVM.PauseJob(_job.Name);
        }

        private async void StopJob()
        {
            await _mainViewModel.ListVM.StopJob(_job.Name);
        }

        private void SubscribeToJobEvents()
        {
            if (_job != null)
            {
                _job.PropertyChanged += Job_PropertyChanged;
            }
        }

        public void UnsubscribeFromJobEvents()
        {
            if (_job != null)
            {
                _job.PropertyChanged -= Job_PropertyChanged;
            }
        }

        private void Job_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
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
                    (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PauseJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    break;
                case nameof(backupJob.ErrorMessage):
                    OnPropertyChanged(nameof(ErrorMessage));
                    break;
            }
        }
    }
}