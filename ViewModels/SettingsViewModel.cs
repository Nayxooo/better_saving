using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;

namespace better_saving.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;        private string _blockedSoftwareText;
        private string _fileExtensionsText;


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

            _mainVM.CurrentView = null;
        }

        private void Cancel()
        {
            _mainVM.CurrentView = null;
        }
    }
}
