using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using better_saving.Models; // Added for TCPServer

namespace better_saving.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;
        private string _blockedSoftwareText;
        private string _fileExtensionsText;
        private string _priorityFileExtensionsText;
        private string _maxFileTranferSizeText;
        private bool _isTcpServerEnabled;
        private string _tcpServerAddress;
        private bool _hasUnsavedChanges;

        public string SettingsTitle => HasUnsavedChanges ? GetLocalized("SettingsTitle") + "*" : GetLocalized("SettingsTitle");

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    OnPropertyChanged(nameof(SettingsTitle)); // Notify that SettingsTitle has changed
                }
            }
        }

        public string BlockedSoftwareText
        {
            get => _blockedSoftwareText;
            set
            {
                if (SetProperty(ref _blockedSoftwareText, value))
                    HasUnsavedChanges = true;
            }
        }

        public string FileExtensionsText
        {
            get => _fileExtensionsText;
            set
            {
                if (SetProperty(ref _fileExtensionsText, value))
                    HasUnsavedChanges = true;
            }
        }

        public string PriorityFileExtensionsText
        {
            get => _priorityFileExtensionsText;
            set
            {
                if (SetProperty(ref _priorityFileExtensionsText, value))
                    HasUnsavedChanges = true;
            }
        }

        public string MaxFileTranferSizeText
        {
            get => _maxFileTranferSizeText;
            set
            {
                if (SetProperty(ref _maxFileTranferSizeText, value))
                    HasUnsavedChanges = true;
            }
        }

        public bool IsTcpServerEnabled
        {
            get => _isTcpServerEnabled;
            set
            {
                if (SetProperty(ref _isTcpServerEnabled, value))
                {
                    _mainVM.ToggleTcpServer(value);
                    TcpServerAddress = _mainVM.GetTcpServerAddress(); // Update address when server state changes
                    HasUnsavedChanges = true;
                }
            }
        }

        public string TcpServerAddress
        {
            get => _tcpServerAddress;
            set => SetProperty(ref _tcpServerAddress, value);
        }

        public bool IsCurrentLanguageFR => _mainVM.SelectedLanguage == "fr-FR";

        public bool IsCurrentLanguageEN => _mainVM.SelectedLanguage == "en-US";


        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public SettingsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _blockedSoftwareText = string.Join(",", _mainVM.GetBlockedSoftware());
            _fileExtensionsText = string.Join(",", _mainVM.GetFileExtensions());
            _priorityFileExtensionsText = string.Join(",", _mainVM.GetPriorityFileExtensions());
            _maxFileTranferSizeText = _mainVM.GetMaxFileTransferSize().ToString();
            _isTcpServerEnabled = _mainVM.IsTcpServerRunning();
            _tcpServerAddress = _mainVM.GetTcpServerAddress();

            HasUnsavedChanges = false; // Initialize as false

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            SetLanguageCommand = _mainVM.ChangeLanguageCommand;
        }
        private void Save()
        {
            var softwareList = BlockedSoftwareText
                               .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToList();

            _mainVM.SetBlockedSoftware(softwareList);

            var extensions = FileExtensionsText
                             .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                             .Select(e => e.Trim().ToLower())
                             .Where(e => e.StartsWith("."))
                             .ToList();

            _mainVM.SetFileExtensions(extensions);

            var priorityExtensions = PriorityFileExtensionsText
                                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                                    .Select(e => e.Trim().ToLower())
                                    .Where(e => e.StartsWith("."))
                                    .ToList();

            _mainVM.SetPriorityFileExtensions(priorityExtensions);

            if (string.IsNullOrWhiteSpace(MaxFileTranferSizeText)) _mainVM.SetMaxFileTransferSize(0);
            else if (int.TryParse(MaxFileTranferSizeText, out int maxSize)) _mainVM.SetMaxFileTransferSize(maxSize);

            _mainVM.ToggleTcpServer(IsTcpServerEnabled);
            TcpServerAddress = _mainVM.GetTcpServerAddress();

            // Save all settings including TCP server state
            var currentSettings = Settings.LoadSettings(); // Load current to preserve other settings
            currentSettings.BlockedSoftware = softwareList;
            currentSettings.FileExtensions = extensions;
            currentSettings.PriorityFileExtensions = priorityExtensions;
            currentSettings.MaxFileTransferSize = (string.IsNullOrWhiteSpace(MaxFileTranferSizeText) || !int.TryParse(MaxFileTranferSizeText, out int parsedMaxSize)) ? 0 : parsedMaxSize;
            currentSettings.Language = _mainVM.SelectedLanguage; // Assuming MainViewModel holds the current language
            currentSettings.IsTcpServerEnabled = IsTcpServerEnabled;
            Settings.SaveSettings(currentSettings);

            HasUnsavedChanges = false; // Reset after saving
        }

        private void Cancel()
        {
            // Reset fields to original values before closing
            _blockedSoftwareText = string.Join(",", _mainVM.GetBlockedSoftware());
            _fileExtensionsText = string.Join(",", _mainVM.GetFileExtensions());
            _priorityFileExtensionsText = string.Join(",", _mainVM.GetPriorityFileExtensions());
            _maxFileTranferSizeText = _mainVM.GetMaxFileTransferSize().ToString();
            _isTcpServerEnabled = _mainVM.IsTcpServerRunning();
            // Notify changes to reset UI
            OnPropertyChanged(nameof(BlockedSoftwareText));
            OnPropertyChanged(nameof(FileExtensionsText));
            OnPropertyChanged(nameof(PriorityFileExtensionsText));
            OnPropertyChanged(nameof(MaxFileTranferSizeText));
            OnPropertyChanged(nameof(IsTcpServerEnabled));

            HasUnsavedChanges = false; // Reset unsaved changes flag
            _mainVM.CurrentView = null;
        }

        // Helper method to get localized string (assuming you have such a mechanism)
        private static string GetLocalized(string key)
        {
            // This is a placeholder. You'll need to implement this based on your localization setup.
            // For example, using Application.Current.FindResource(key) as string;
            // Or a more robust localization service.
            if (System.Windows.Application.Current.TryFindResource(key) is string localizedValue)
            {
                return localizedValue;
            }
            return key; // Fallback to key if not found
        }
    }
}
