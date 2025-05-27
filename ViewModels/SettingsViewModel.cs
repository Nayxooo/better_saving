using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;

namespace better_saving.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;
        private string _blockedSoftwareText;
        private string _fileExtensionsText;
        private string _priorityFileExtensionsText;

        private string _maxFileTranferSizeText;


        public string BlockedSoftwareText
        {
            get => _blockedSoftwareText;
            set => SetProperty(ref _blockedSoftwareText, value);
        }

        public string FileExtensionsText
        {
            get => _fileExtensionsText;
            set => SetProperty(ref _fileExtensionsText, value);
        }

        public string PriorityFileExtensionsText
        {
            get => _priorityFileExtensionsText;
            set => SetProperty(ref _priorityFileExtensionsText, value);
        }

        public string MaxFileTranferSizeText
        {
            get => _maxFileTranferSizeText;
            set => SetProperty(ref _maxFileTranferSizeText, value);
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

            // Save settings to file and encrypt files with the specified extensions
            _mainVM.SetFileExtensions(extensions);

            var priorityExtensions = PriorityFileExtensionsText
                                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                                    .Select(e => e.Trim().ToLower())
                                    .Where(e => e.StartsWith("."))
                                    .ToList();

            // Save priority file extensions
            _mainVM.SetPriorityFileExtensions(priorityExtensions);

            // Save max file transfer size
            // if the text is empty, set it to 0
            if (string.IsNullOrWhiteSpace(MaxFileTranferSizeText)) _mainVM.SetMaxFileTransferSize(0);
            else if (int.TryParse(MaxFileTranferSizeText, out int maxSize)) _mainVM.SetMaxFileTransferSize(maxSize);
            
            _mainVM.CurrentView = null;
        }

        private void Cancel() { _mainVM.CurrentView = null; }
    }
}
