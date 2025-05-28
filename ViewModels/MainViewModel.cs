using better_saving.Models;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace better_saving.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private BackupListViewModel _listVM;
        private ViewModelBase? _currentView;
        private ViewModelBase? PreviousView;

        private List<string> _blockedSoftware = [];
        private string? _runningBlockedSoftware;
        private string _selectedLanguage;
        private ObservableCollection<backupJob> _blockedJobs = new ObservableCollection<backupJob>();
        private readonly TCPServer _tcpServer;
        private const string StateFileName = "state.json"; // Define state file name

        private long _currentGlobalTransferringSizeInBytes = 0; // Tracks current total size of files being transferred, in bytes
        public long CurrentGlobalTransferringSizeInBytes => _currentGlobalTransferringSizeInBytes; // Public getter

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
                if (value == null && PreviousView != null)
                {
                    value = PreviousView; // Restore previous view if current is null
                }
                if (value != _currentView)
                {
                    PreviousView = _currentView; // Save current view as previous before changing
                }
                SetProperty(ref _currentView, value);
            }
        }

        public ICommand ShowCreateJobViewCommand { get; }
        public ICommand ToggleSettingsViewCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    SetProperty(ref _selectedLanguage, value);
                    ChangeLanguage(_selectedLanguage);
                }
            }
        }

        public ObservableCollection<backupJob> BlockedJobs => _blockedJobs;

        public MainViewModel()
        {
            _tcpServer = new TCPServer(this);
            _listVM = new BackupListViewModel(this, _tcpServer); // Pass TCPServer to BackupListViewModel
            ShowCreateJobViewCommand = new RelayCommand(_ => ShowCreateJobViewInternal());
            ToggleSettingsViewCommand = new RelayCommand(_ => ToggleSettingsViewInternal());
            ChangeLanguageCommand = new RelayCommand(param => SelectedLanguage = param?.ToString() ?? "en");
            ExecuteCommand = new RelayCommand(param => Execute(param as backupJob));
            PauseCommand = new RelayCommand(param => Pause(param as backupJob), param => CanPause(param as backupJob));
            StopCommand = new RelayCommand(param => Stop(param as backupJob), param => CanStop(param as backupJob));            // Load settings from file
            var settings = Settings.LoadSettings();
            _blockedSoftware = settings.BlockedSoftware;
            _selectedLanguage = settings.Language;

            // Apply loaded language
            ChangeLanguage(_selectedLanguage);

            // Start TCP server if enabled in settings
            if (settings.IsTcpServerEnabled)
            {
                ToggleTcpServer(true);
            }

            // Initialiser le monitoring des tâches bloquées
            InitializeBlockedJobsMonitoring();

            _listVM.GetLogger().LogBackupDetails("System", "Settings",
                $"Settings loaded - Blocked software: {(_blockedSoftware.Count != 0 ? string.Join(", ", _blockedSoftware) : "(empty)")}", 0, 0, 0);
        }

        internal void ShowBA()
        {
            CurrentView = new BAViewModel(this);
        }

        internal void ShowCreateJobViewInternal()
        {
            CurrentView = new BackupCreationViewModel(this);
        }

        internal void ToggleSettingsViewInternal()
        {
            if (CurrentView is SettingsViewModel)
            {
                // If already in settings view, go back to previous view
                CurrentView = PreviousView;
            }
            else
            {
                CurrentView = new SettingsViewModel(this);
            }

        }

        public void ShowJobStatus(backupJob selectedJob)
        {
            CurrentView = new BackupStatusViewModel(selectedJob, this);
        }


        public void SetBlockedSoftware(List<string> softwareList)
        {
            _blockedSoftware = softwareList ?? [];
            _listVM.GetLogger().LogBackupDetails("System", "Settings",
                $"Blocked software updated to: {(_blockedSoftware.Count != 0 ? string.Join(", ", _blockedSoftware) : "(empty)")}", 0, 0, 0);
            (_listVM.StartAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        public List<string> GetBlockedSoftware()
        {
            return _blockedSoftware;
        }

        public void SetFileExtensions(List<string> extensions)
        {
            // Save the file extensions for future use
            var settings = Settings.LoadSettings();
            settings.FileExtensions = extensions;
            settings.BlockedSoftware = _blockedSoftware;
            settings.Language = _selectedLanguage;
            Settings.SaveSettings(settings);

            _listVM.GetLogger().LogBackupDetails("System", "Settings",
                $"File extensions updated to: {(extensions.Count != 0 ? string.Join(", ", extensions) : "(empty)")}", 0, 0, 0);
        }
        public List<string> GetFileExtensions()
        {
            var settings = Settings.LoadSettings();
            return settings.FileExtensions;
        }

        public void SetPriorityFileExtensions(List<string> extensions)
        {
            // Save the priority file extensions for future use
            var settings = Settings.LoadSettings();
            settings.PriorityFileExtensions = extensions;
            settings.BlockedSoftware = _blockedSoftware;
            settings.FileExtensions = GetFileExtensions();
            settings.Language = _selectedLanguage;
            Settings.SaveSettings(settings);

            _listVM.GetLogger().LogBackupDetails("System", "Settings",
                $"Priority file extensions updated to: {(extensions.Count != 0 ? string.Join(", ", extensions) : "(empty)")}", 0, 0, 0);
        }
        public List<string> GetPriorityFileExtensions()
        {
            var settings = Settings.LoadSettings();
            return settings.PriorityFileExtensions;
        }

        public void SetMaxFileTransferSize(int size)
        {
            // Save the max file transfer size for future use
            var settings = Settings.LoadSettings();
            settings.MaxFileTransferSize = size;
            settings.BlockedSoftware = _blockedSoftware;
            settings.FileExtensions = GetFileExtensions();
            settings.PriorityFileExtensions = GetPriorityFileExtensions();
            settings.Language = _selectedLanguage;
            Settings.SaveSettings(settings);

            _listVM.GetLogger().LogBackupDetails("System", "Settings",
                $"Max file transfer size updated to: {size} KiloBytes", 0, 0, 0);
        }
        public int GetMaxFileTransferSize()
        {
            var settings = Settings.LoadSettings();
            return settings.MaxFileTransferSize;
        }

        public void ToggleTcpServer(bool enable)
        {
            if (enable)
            {
                _tcpServer.Start();
            }
            else
            {
                _tcpServer.Stop();
            }
            OnPropertyChanged(nameof(IsTcpServerRunning)); // Notify that server status might have changed
            OnPropertyChanged(nameof(GetTcpServerAddress)); // Notify that server address might have changed
        }

        public bool IsTcpServerRunning()
        {
            return _tcpServer.IsRunning;
        }

        public string GetTcpServerAddress()
        {
            return _tcpServer.GetServerAddress();
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
                _listVM.GetLogger().LogBackupDetails("System", "SoftwareCheck",
                    $"Error checking software: {ex.Message}", 0, 0, 0);
                return false;
            }
        }

        public string? GetRunningBlockedSoftware()
        {
            return _runningBlockedSoftware;
        }

        private void ChangeLanguage(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                var dict = new ResourceDictionary();
                switch (languageCode)
                {
                    case "fr":
                    case "fr-FR":
                        dict.Source = new Uri("..\\Resources\\Localization\\Strings.fr-FR.xaml", UriKind.Relative);
                        languageCode = "fr-FR";
                        break;

                    case "en":
                    case "en-US":
                    default:
                        dict.Source = new Uri("..\\Resources\\Localization\\Strings.en-US.xaml", UriKind.Relative);
                        languageCode = "en-US";
                        break;
                }

                var old = System.Windows.Application.Current.Resources.MergedDictionaries
                             .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") == true);
                if (old != null) System.Windows.Application.Current.Resources.MergedDictionaries.Remove(old);

                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

                // If SettingsViewModel is currently displayed, refresh it
                if (CurrentView is SettingsViewModel)
                {
                    CurrentView = new SettingsViewModel(this);
                }

                var settings = Settings.LoadSettings();
                settings.Language = languageCode;
                settings.BlockedSoftware = _blockedSoftware;
                Settings.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails("System", "Language", $"Error changing language: {ex.Message}", 0, 0, 0);
            }
        }

        private void AddToBlockedJobs(backupJob job)
        {
            // Run on UI thread using a more explicit approach
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (!_blockedJobs.Contains(job))
                    {
                        _blockedJobs.Add(job);
                    }
                }));
            }
            else
            {
                // Fallback if Application.Current is null
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    if (!_blockedJobs.Contains(job))
                    {
                        _blockedJobs.Add(job);
                    }
                });
            }
        }

        public void CheckBlockedJobs()
        {
            // Check if all blocking software is closed
            if (!IsSoftwareRunning())
            {
                // Make a copy to avoid collection modification during enumeration
                var jobsToResume = _blockedJobs.ToList();
                foreach (var job in jobsToResume)
                {
                    // Only auto-resume jobs that were paused due to blocking software
                    if (job.State == JobStates.Paused)
                    {
                        // Démarrer une tâche pour le resume asynchrone
                        Task.Run(async () =>
                        {
                            await job.Resume();
                        });
                        _blockedJobs.Remove(job);
                    }
                }
            }
        }

        private void InitializeBlockedJobsMonitoring()
        {
            StartBlockedSoftwareMonitoring();
        }

        private void StartBlockedSoftwareMonitoring()
        {
            // Vérifier périodiquement si les logiciels bloquants sont fermés
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (sender, e) => CheckBlockedJobs();
            timer.Start();
        }

        public backupJob CreateNewJob(string name, string sourceDir, string targetDir, JobType type)
        {
            return new backupJob(
                name,
                sourceDir,
                targetDir,
                type,
                _listVM.GetLogger(),
                IsSoftwareRunning, // Pass the method to check if blocking software is running
                AddToBlockedJobs   // Pass the callback to add to blocked jobs
            );
        }

        /// <summary>
        /// Increments the global counter for data currently being transferred.
        /// </summary>
        /// <param name="fileSizeInBytes">Size of the file starting to transfer, in bytes.</param>
        public void IncrementGlobalTransferringSize(long fileSizeInBytes)
        {
            Interlocked.Add(ref _currentGlobalTransferringSizeInBytes, fileSizeInBytes);
        }

        /// <summary>
        /// Decrements the global counter for data currently being transferred.
        /// </summary>
        /// <param name="fileSizeInBytes">Size of the file that finished transferring, in bytes.</param>
        public void DecrementGlobalTransferringSize(long fileSizeInBytes)
        {
            Interlocked.Add(ref _currentGlobalTransferringSizeInBytes, -fileSizeInBytes);
        }

        /// <summary>
        /// Checks if a file of a given size can be transferred based on the global limit.
        /// </summary>
        /// <param name="fileSizeInBytes">Size of the file to be transferred, in bytes.</param>
        /// <returns>True if the file can be transferred, false otherwise.</returns>
        public bool CanTransferFile(long fileSizeInBytes)
        {
            // Assuming GetMaxFileTransferSize() returns the limit in Kilobytes (long)
            // And a value of 0 means unlimited.
            long maxAllowedKB = GetMaxFileTransferSize(); // This method must exist and return the limit in KB.

            if (maxAllowedKB == 0) // 0 KB means unlimited transfer
            {
                return true;
            }

            // if (maxAllowedKB == 0) return true; // Unlimited transfer if max size is 0

            long maxAllowedBytes = maxAllowedKB * 1024;
            return (_currentGlobalTransferringSizeInBytes + fileSizeInBytes) <= maxAllowedBytes;
        }

        private void Execute(backupJob? selectedJob)
        {
            try
            {
                if (selectedJob != null)
                {
                    // The Start() and Resume() methods in backupJob will need to be aware of MainViewModel
                    // to use the new transfer limit logic, ideally by having a reference to MainViewModel.
                    if (selectedJob.State == JobStates.Paused)
                    {
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Resuming job", 0, 0, 0);
                        Task.Run(async () => await selectedJob.Resume()); // If backupJob has MainViewModel ref
                    }
                    else if (selectedJob.State == JobStates.Stopped || selectedJob.State == JobStates.Paused || selectedJob.State == JobStates.Failed)
                    {
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Starting job", 0, 0, 0);
                        Task.Run(async () => await selectedJob.Start()); // If backupJob has MainViewModel ref
                    }
                }
                else
                {
                    foreach (var job in _listVM.Jobs)
                    {
                        if (job.State == JobStates.Paused)
                        {
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Resuming job", 0, 0, 0);
                            Task.Run(async () => await job.Resume()); // If backupJob has MainViewModel ref
                        }
                        else if (job.State == JobStates.Stopped || job.State == JobStates.Paused || job.State == JobStates.Failed)
                        {
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Starting job", 0, 0, 0);
                            Task.Run(async () => await job.Start()); // If backupJob has MainViewModel ref
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails("System", "Execute", $"Error executing job: {ex.Message}", 0, 0, 0);
            }
        }

        private void Pause(backupJob? selectedJob)
        {
            try
            {
                if (selectedJob != null)
                {
                    if (selectedJob.State == JobStates.Working || selectedJob.State == JobStates.Stopped)
                    {
                        selectedJob.Pause();
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Pausing job", 0, 0, 0);
                    }
                }
                else
                {
                    foreach (var job in _listVM.Jobs)
                    {
                        if (job.State == JobStates.Working || job.State == JobStates.Stopped)
                        {
                            job.Pause();
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Pausing job", 0, 0, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails("System", "Pause", $"Error pausing job: {ex.Message}", 0, 0, 0);
            }
        }

        private bool CanPause(backupJob? selectedJob)
        {
            if (selectedJob != null)
            {
                return selectedJob.State == JobStates.Working || selectedJob.State == JobStates.Stopped;
            }
            return _listVM.Jobs.Any(job => job.State == JobStates.Working || job.State == JobStates.Stopped);
        }

        private void Stop(backupJob? selectedJob)
        {
            try
            {
                if (selectedJob != null)
                {
                    if (selectedJob.State == JobStates.Working || selectedJob.State == JobStates.Paused || selectedJob.State == JobStates.Stopped)
                    {
                        selectedJob.Stop();
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Stopping job", 0, 0, 0);
                    }
                }
                else
                {
                    foreach (var job in _listVM.Jobs)
                    {
                        if (job.State == JobStates.Working || job.State == JobStates.Paused || job.State == JobStates.Stopped)
                        {
                            job.Stop();
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Stopping job", 0, 0, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails("System", "Stop", $"Error stopping job: {ex.Message}", 0, 0, 0);
            }
        }

        private bool CanStop(backupJob? selectedJob)
        {
            if (selectedJob != null)
            {
                return selectedJob.State == JobStates.Working || selectedJob.State == JobStates.Paused || selectedJob.State == JobStates.Stopped;
            }
            return _listVM.Jobs.Any(job => job.State == JobStates.Working || job.State == JobStates.Paused || job.State == JobStates.Stopped);
        }
    }
}
