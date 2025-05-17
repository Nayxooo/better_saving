using better_saving.Models; // Add this for backupJob
using System.Windows.Input; // Required for ICommand
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace better_saving.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private BackupListViewModel _listVM;
        private ViewModelBase? _currentView;
        // private readonly Logger _logger; // Logger instance, if needed for specific logging tasks
        private List<string> _blockedSoftware = new List<string>(); // Modified: Empty list by default
        private string? _runningBlockedSoftware;

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
        public ICommand ShowSettingsViewCommand { get; }

        public MainViewModel()
        {
            // _logger = new Logger(); // Instantiate if specific logging methods of Logger are used here

            _listVM = new BackupListViewModel(this); // Pass MainViewModel instance
            ShowCreateJobViewCommand = new RelayCommand(param => ShowCreateJobViewInternal());
            ShowSettingsViewCommand = new RelayCommand(param => ShowSettingsViewInternal());
            CurrentView = null;

            // Modified: Log empty blocked software list
            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings", "Blocked software initialized to: (empty)", 0, 0);
        }

        internal void ShowCreateJobViewInternal() // Changed to internal and renamed for clarity
        {
            CurrentView = new BackupCreationViewModel(this); // Pass MainViewModel instance
        }

        internal void ShowSettingsViewInternal()
        {
            CurrentView = new SettingsViewModel(this);
        }

        public void ShowJobStatus(backupJob selectedJob) 
        {
            CurrentView = new BackupStatusViewModel(selectedJob, this);
        }

        // Modified: Allow empty software list
        public void SetBlockedSoftware(List<string> softwareList)
        {
            _blockedSoftware = softwareList ?? new List<string>();
            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings", $"Blocked software updated to: {(softwareList.Any() ? string.Join(", ", softwareList) : "(empty)")}", 0, 0);
            (_listVM.StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public List<string> GetBlockedSoftware()
        {
            return _blockedSoftware;
        }

        public bool IsSoftwareRunning()
        {
            try
            {
                _runningBlockedSoftware = null;
                foreach (var software in _blockedSoftware)
                {
                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(software)).Any())
                    {
                        _runningBlockedSoftware = software;
                        return true;
                    }
                }
                return false;
            }
            catch (System.Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "SoftwareCheck", $"Error checking software: {ex.Message}", 0, 0);
                return false;
            }
        }

        public string? GetRunningBlockedSoftware()
        {
            return _runningBlockedSoftware;
        }
    }
}