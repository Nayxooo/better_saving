using better_saving.Models;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Linq;

namespace better_saving.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private BackupListViewModel _listVM;
        private ViewModelBase? _currentView;
        private List<string> _blockedSoftware = [];
        private string? _runningBlockedSoftware;
        private string _selectedLanguage;

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
        public ICommand ChangeLanguageCommand { get; }

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
        public MainViewModel()
        {
            _listVM = new BackupListViewModel(this);
            ShowCreateJobViewCommand = new RelayCommand(_ => ShowCreateJobViewInternal());
            ShowSettingsViewCommand = new RelayCommand(_ => ShowSettingsViewInternal());
            ChangeLanguageCommand = new RelayCommand(param => SelectedLanguage = param?.ToString() ?? "en");

            // Load settings from file
            var settings = Settings.LoadSettings();
            _blockedSoftware = settings.BlockedSoftware;
            _selectedLanguage = settings.Language;

            // Apply loaded language
            ChangeLanguage(_selectedLanguage);

            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings",
                $"Settings loaded - Blocked software: {(_blockedSoftware.Count != 0 ? string.Join(", ", _blockedSoftware) : "(empty)")}", 0, 0);
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
            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings",
                $"Blocked software updated to: {(_blockedSoftware.Count != 0 ? string.Join(", ", _blockedSoftware) : "(empty)")}", 0, 0);
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

            // Encrypt the files with the provided extensions
            EncryptFilesInLogs(extensions);
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

            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings",
                $"Priority file extensions updated to: {(extensions.Count != 0 ? string.Join(", ", extensions) : "(empty)")}", 0, 0);
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
                _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "SoftwareCheck",
                    $"Error checking software: {ex.Message}", 0, 0);
                return false;
            }
        }

        public string? GetRunningBlockedSoftware()
        {
            return _runningBlockedSoftware;
        }

        public void EncryptFilesInLogs(List<string> extensions)
        {
            string logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logsPath))
                return;

            var filesToEncrypt = Directory
                .EnumerateFiles(logsPath, "*.*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                .ToList();

            foreach (var file in filesToEncrypt)
            {
                try
                {
                    string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");
                    string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                    if (!File.Exists(exePath))
                    {
                        System.Windows.MessageBox.Show($"CryptoSoft.exe introuvable : {exePath}");
                        return;
                    }

                    if (!File.Exists(keyPath))
                    {
                        System.Windows.MessageBox.Show("Le fichier appsettings.json est introuvable.");
                        return;
                    }

                    ProcessStartInfo psi = new()
                    {
                        FileName = exePath,
                        Arguments = $"\"{file}\" \"{keyPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? proc = Process.Start(psi);
                    if (proc == null)
                    {
                        System.Windows.MessageBox.Show("Erreur lors du démarrage de CryptoSoft.");
                        return;
                    }
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        System.Windows.MessageBox.Show("Erreur CryptoSoft : " + error);
                        _listVM.GetLogger().LogBackupDetails(DateTime.Now.ToString("o"), "Crypto", "EncryptError", error, 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    _listVM.GetLogger().LogBackupDetails(DateTime.Now.ToString("o"), "Crypto", "Exception", ex.Message, 0, 0);
                }
            }
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

                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);                // Update language in settings file
                var settings = Settings.LoadSettings();
                settings.Language = languageCode;
                settings.BlockedSoftware = _blockedSoftware;
                Settings.SaveSettings(settings);

                // If SettingsViewModel is currently displayed, refresh it
                if (CurrentView is SettingsViewModel)
                {
                    CurrentView = new SettingsViewModel(this);
                }
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails(DateTime.Now.ToString("o"),
                    "System", "Language", $"Error changing language: {ex.Message}", 0, 0);
            }
        }
    }
}
