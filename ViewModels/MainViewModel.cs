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
        private List<string> _blockedSoftware = [];
        private string? _runningBlockedSoftware;
        private string _selectedLanguage;
        private ObservableCollection<backupJob> _blockedJobs = new ObservableCollection<backupJob>();

        public BackupListViewModel ListVM
        {
            get => _listVM;
            set => SetProperty(ref _listVM, value);
        }

        public ViewModelBase? CurrentView
        {
            get => _currentView;
            set { SetProperty(ref _currentView, value); }
        }

        public ICommand ShowCreateJobViewCommand { get; }
        public ICommand ShowSettingsViewCommand { get; }
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
            _listVM = new BackupListViewModel(this);
            ShowCreateJobViewCommand = new RelayCommand(_ => ShowCreateJobViewInternal());
            ShowSettingsViewCommand = new RelayCommand(_ => ShowSettingsViewInternal());
            ChangeLanguageCommand = new RelayCommand(param => SelectedLanguage = param?.ToString() ?? "en");
            ExecuteCommand = new RelayCommand(param => Execute(param as backupJob));
            PauseCommand = new RelayCommand(param => Pause(param as backupJob), param => CanPause(param as backupJob));
            StopCommand = new RelayCommand(param => Stop(param as backupJob), param => CanStop(param as backupJob));

            // Load settings from file
            var settings = Settings.LoadSettings();
            _blockedSoftware = settings.BlockedSoftware;
            _selectedLanguage = settings.Language;

            // Apply loaded language
            ChangeLanguage(_selectedLanguage);
            
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

        internal void ShowSettingsViewInternal()
        {
            CurrentView = new SettingsViewModel(this);
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

        private void Execute(backupJob? selectedJob)
        {
            try
            {
                if (selectedJob != null)
                {
                    if (selectedJob.State == JobStates.Paused)
                    {
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Resuming job", 0, 0, 0);
                        Task.Run(async () => await selectedJob.Resume());
                    }
                    else if (selectedJob.State == JobStates.Stopped || selectedJob.State == JobStates.Paused || selectedJob.State == JobStates.Failed)
                    {
                        _listVM.GetLogger().LogBackupDetails(selectedJob.Name, "System", "Starting job", 0, 0, 0);
                        Task.Run(async () => await selectedJob.Start());
                    }
                }
                else
                {
                    foreach (var job in _listVM.Jobs)
                    {
                        if (job.State == JobStates.Paused)
                        {
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Resuming job", 0, 0, 0);
                            Task.Run(async () => await job.Resume());
                        }
                        else if (job.State == JobStates.Stopped || job.State == JobStates.Paused || job.State == JobStates.Failed)
                        {
                            _listVM.GetLogger().LogBackupDetails(job.Name, "System", "Starting job", 0, 0, 0);
                            Task.Run(async () => await job.Start());
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
