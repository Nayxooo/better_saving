using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EasySave.Models;

namespace EasySave.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Job> Jobs { get; set; }

        private Job _selectedJob;
        public Job SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged();
                if (value != null)
                    CurrentView = value;
            }
        }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private JobCreationViewModel _jobCreationViewModel;
        public JobCreationViewModel JobCreationViewModel
        {
            get => _jobCreationViewModel;
            set { _jobCreationViewModel = value; OnPropertyChanged(); }
        }

        private bool _isPopupVisible;
        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set { _isPopupVisible = value; OnPropertyChanged(); }
        }

        private string _popupTitle;
        public string PopupTitle
        {
            get => _popupTitle;
            set { _popupTitle = value; OnPropertyChanged(); }
        }

        private string _popupInput;
        public string PopupInput
        {
            get => _popupInput;
            set { _popupInput = value; OnPropertyChanged(); }
        }

        private string _currentLanguage = "EN";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set { _currentLanguage = value; OnPropertyChanged(); }
        }

        public ICommand NavigateToCreateJobCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand ToggleJobCommand { get; }
        public ICommand LaunchAllJobsCommand { get; }
        public ICommand SortJobsCommand { get; }
        public ICommand ShowEncryptionInputCommand { get; }
        public ICommand ShowBlockingProgramsInputCommand { get; }
        public ICommand SavePopupInputCommand { get; }
        public ICommand SetLanguageCommand { get; }

        public MainViewModel()
        {
            Jobs = new ObservableCollection<Job>();
            JobCreationViewModel = new JobCreationViewModel();

            // Initialize commands
            NavigateToCreateJobCommand = new RelayCommand(() => CurrentView = JobCreationViewModel);
            CreateJobCommand = new RelayCommand(CreateJob);
            DeleteJobCommand = new RelayCommand(DeleteJob);
            ToggleJobCommand = new RelayCommand(ToggleJob);
            LaunchAllJobsCommand = new RelayCommand(() => { /* Placeholder */ });
            SortJobsCommand = new RelayCommand(() => { /* Placeholder */ });
            ShowEncryptionInputCommand = new RelayCommand(() =>
            {
                PopupTitle = "Encryption";
                PopupInput = "";
                IsPopupVisible = true;
            });
            ShowBlockingProgramsInputCommand = new RelayCommand(() =>
            {
                PopupTitle = "Blocking programs";
                PopupInput = "";
                IsPopupVisible = true;
            });
            SavePopupInputCommand = new RelayCommand(() => IsPopupVisible = false);
            SetLanguageCommand = new RelayCommand<string>(lang => CurrentLanguage = lang);

            // Add dummy jobs
            Jobs.Add(new Job { Name = "Job 1", SourceDirectory = "C:\\Source1", TargetDirectory = "C:\\Target1", BackupType = "Full", Progress = 50, Status = "Running" });
            Jobs.Add(new Job { Name = "Job 2", SourceDirectory = "C:\\Source2", TargetDirectory = "C:\\Target2", BackupType = "Differential", Progress = 100, Status = "Completed" });

            // Set initial view
            if (Jobs.Count > 0)
            {
                SelectedJob = Jobs[0];
                CurrentView = SelectedJob;
            }
        }

        private void CreateJob()
        {
            var newJob = new Job
            {
                Name = JobCreationViewModel.Name,
                SourceDirectory = JobCreationViewModel.SourceDirectory,
                TargetDirectory = JobCreationViewModel.TargetDirectory,
                BackupType = JobCreationViewModel.BackupType,
                Status = "Paused"
            };
            Jobs.Add(newJob);
            SelectedJob = newJob;
            CurrentView = newJob;
        }

        private void DeleteJob()
        {
            if (SelectedJob != null)
            {
                Jobs.Remove(SelectedJob);
                SelectedJob = null;
                CurrentView = null;
            }
        }

        private void ToggleJob()
        {
            if (SelectedJob != null)
            {
                SelectedJob.Status = SelectedJob.Status == "Running" ? "Paused" : "Running";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}