using System.Collections.Generic;
using System.Windows.Input;

namespace better_saving.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private string _blockedSoftwareText;

        public string BlockedSoftwareText
        {
            get => _blockedSoftwareText;
            set => SetProperty(ref _blockedSoftwareText, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public SettingsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _blockedSoftwareText = string.Join(",", _mainViewModel.GetBlockedSoftware());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Save()
        {
            var softwareList = BlockedSoftwareText
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            _mainViewModel.SetBlockedSoftware(softwareList);
            _mainViewModel.CurrentView = null;
        }

        private void Cancel()
        {
            _mainViewModel.CurrentView = null;
        }
    }
}