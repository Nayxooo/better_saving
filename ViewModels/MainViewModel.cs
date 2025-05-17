using better_saving.Models; // Add this for backupJob
using System.Windows.Input; // Required for ICommand

namespace better_saving.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private BackupListViewModel _listVM;
        private ViewModelBase? _currentView;
        // private readonly Logger _logger; // Logger instance, if needed for specific logging tasks

        public BackupListViewModel ListVM
        {
            get => _listVM;
            set => SetProperty(ref _listVM, value);
        }

        public ViewModelBase? CurrentView
        {
            get => _currentView;
            set
            {
                if (_currentView is BackupStatusViewModel oldStatusVM)
                {
                    oldStatusVM.UnsubscribeFromJobEvents();
                }
                SetProperty(ref _currentView, value);
            }
        }

        public ICommand ShowCreateJobViewCommand { get; }

        public MainViewModel()
        {
            // _logger = new Logger(); // Instantiate if specific logging methods of Logger are used here
            
            _listVM = new BackupListViewModel(this); // Pass MainViewModel instance
            ShowCreateJobViewCommand = new RelayCommand(param => ShowCreateJobViewInternal()); // Changed to internal method
            CurrentView = null; 
        }

        internal void ShowCreateJobViewInternal() // Changed to internal and renamed for clarity
        {
            CurrentView = new BackupCreationViewModel(this); // Pass MainViewModel instance
        }

        public void ShowJobStatus(backupJob selectedJob)
        {
            CurrentView = new BackupStatusViewModel(selectedJob, this); // Pass 'this' (MainViewModel instance)
        }
    }
}
