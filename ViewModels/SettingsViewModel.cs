using System.Linq;
using System.Windows.Input;

namespace better_saving.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;
        private string _blockedSoftwareText;

        #region Propri�t�s li�es � l�interface

        public string BlockedSoftwareText
        {
            get => _blockedSoftwareText;
            set => SetProperty(ref _blockedSoftwareText, value);
        }

        #endregion

        #region Commandes

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Commande appel�e par les boutons � FR � / � EN �.
        /// </summary>
        public ICommand SetLanguageCommand { get; }

        #endregion

        public SettingsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            // Texte initial (= liste bloqu�e courante)
            _blockedSoftwareText = string.Join(",", _mainVM.GetBlockedSoftware());

            /*---------- Commandes ----------*/
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            SetLanguageCommand = _mainVM.ChangeLanguageCommand;
        }

        /*------------------ M�thodes priv�es ------------------*/

        private void Save()
        {
            var softwareList = BlockedSoftwareText
                               .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToList();

            _mainVM.SetBlockedSoftware(softwareList);

            // On ferme la vue ( CurrentView = null => retour � la liste )
            _mainVM.CurrentView = null;
        }

        private void Cancel()
        {
            _mainVM.CurrentView = null;
        }
    }
}
