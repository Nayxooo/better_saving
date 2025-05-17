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
        private List<string> _blockedSoftware = new List<string>();
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

            _selectedLanguage = "en"; // Default language
            ChangeLanguage(_selectedLanguage); // Initialize culture
            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings", "Blocked software initialized to: (empty)", 0, 0);
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
            _blockedSoftware = softwareList ?? new List<string>();
            _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "Settings",
                $"Blocked software updated to: {(softwareList.Any() ? string.Join(", ", softwareList) : "(empty)")}", 0, 0);
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
                _listVM.GetLogger().LogBackupDetails(System.DateTime.Now.ToString("o"), "System", "SoftwareCheck",
                    $"Error checking software: {ex.Message}", 0, 0);
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
                // 1) Culture du thread
                var culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // 2) Nouveau dictionnaire
                var dict = new ResourceDictionary();
                switch (languageCode)
                {
                    case "fr":
                    case "fr-FR":
                        dict.Source = new Uri("..\\Resources\\Localization\\Strings.fr-FR.xaml", UriKind.Relative);
                        break;

                    case "en":
                    case "en-US":
                    default:
                        dict.Source = new Uri("..\\Resources\\Localization\\Strings.en-US.xaml", UriKind.Relative);
                        break;
                }

                // 3) Remplace l’ancien
                var old = System.Windows.Application.Current.Resources.MergedDictionaries
                             .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") == true);
                if (old != null) System.Windows.Application.Current.Resources.MergedDictionaries.Remove(old);

                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

                // 4) Rafraîchit la vue Settings pour réévaluer les DynamicResource
                if (CurrentView is SettingsViewModel)
                    CurrentView = new SettingsViewModel(this);
            }
            catch (Exception ex)
            {
                _listVM.GetLogger().LogBackupDetails(DateTime.Now.ToString("o"),
                    "System", "Language", $"Error changing language: {ex.Message}", 0, 0);
            }
        }


    }
}
