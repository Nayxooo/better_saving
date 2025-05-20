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

        #region Propriétés liées à l’interface

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

        #endregion

        #region Commandes

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SetLanguageCommand { get; }

        #endregion

        public SettingsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            _blockedSoftwareText = string.Join(",", _mainVM.GetBlockedSoftware());
            _fileExtensionsText = ""; // valeur initiale vide ou à récupérer depuis une source de config

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

            _mainVM.EncryptFilesInLogs(extensions);

            _mainVM.CurrentView = null;
        }

        private void Cancel()
        {
            _mainVM.CurrentView = null;
        }
    }
}
