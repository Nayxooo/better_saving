using System.Windows.Input;
using System.Xml.Linq;
using better_saving.Models;

namespace better_saving.ViewModels
{
    public class BackupCreationViewModel : ViewModelBase // Inherit from ViewModelBase
    {
        private readonly MainViewModel _mainViewModel; 
        private string _name = string.Empty;
        public string Name 
        { 
            get => _name; 
            set => SetProperty(ref _name, value); // Use SetProperty
        }
        
        private string _sourceDirectory = string.Empty;
        public string SourceDirectory 
        { 
            get => _sourceDirectory; 
            set => SetProperty(ref _sourceDirectory, value); // Use SetProperty
        }
        
        private string _targetDirectory = string.Empty;
        public string TargetDirectory 
        { 
            get => _targetDirectory; 
            set => SetProperty(ref _targetDirectory, value); // Use SetProperty
        }

        private JobType _jobType = JobType.Diff;
        public JobType JobType 
        {
            get => _jobType;
            set
            {
                if (SetProperty(ref _jobType, value)) // Use SetProperty
                {
                    OnPropertyChanged(nameof(IsFullBackup));
                    OnPropertyChanged(nameof(IsDiffBackup));
                }
            }
        }

        public bool IsFullBackup
        {
            get => JobType == JobType.Full;
            set
            {
                if (value)
                {
                    JobType = JobType.Full;
                }
            }
        }

        public bool IsDiffBackup
        {
            get => JobType == JobType.Diff;
            set
            {
                if (value)
                {
                    JobType = JobType.Diff;
                }
            }
        }

        // Commands
        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectSourceDirectoryCommand { get; }
        public ICommand SelectTargetDirectoryCommand { get; }

        public BackupCreationViewModel(MainViewModel mainViewModel) 
        {
            _mainViewModel = mainViewModel;            
            CreateCommand = new RelayCommand(_ => CreateJob(), _ => CanCreateJob());
            CancelCommand = new RelayCommand(_ => Cancel());
            SelectSourceDirectoryCommand = new RelayCommand(_ => SelectDirectory(true));
            SelectTargetDirectoryCommand = new RelayCommand(_ => SelectDirectory(false));
        }

        private bool CanCreateJob()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(SourceDirectory) &&
                   !string.IsNullOrWhiteSpace(TargetDirectory);
        }

        private void SelectDirectory(bool isSourceDirectory)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (isSourceDirectory)
                    SourceDirectory = dialog.SelectedPath;
                else
                    TargetDirectory = dialog.SelectedPath;
            }
        }

        private void CreateJob()
        {
            var newJob = new backupJob(Name, SourceDirectory, TargetDirectory, JobType, new Logger());
            _mainViewModel.ListVM.AddJob(newJob); 
            _mainViewModel.CurrentView = null; 
        }

        private void Cancel()
        {
            _mainViewModel.CurrentView = null; 
        }
    }
}
