using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace better_saving.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly BackupManager _backupManager;

        public IAsyncRelayCommand SaveCommand { get; }

        public MainWindowViewModel()
        {
            _backupManager = new BackupManager();
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }
        private async Task SaveAsync()
        {
            bool success = await _backupManager.SaveAsync();

            if (!success)
            {
                // Sauvegarde bloquée (ex : surcharge réseau)
                System.Diagnostics.Debug.WriteLine("❌ Sauvegarde bloquée (bande passante ou processus critique).");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ Sauvegarde réussie.");
            }
        }
    }
}
