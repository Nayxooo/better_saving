using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using better_saving;

namespace better_saving.ViewModels
{
    public class MainWindow : BaseViewModel
    {
        private readonly BackupManager _BackupManage = new BackupManager();
        private BackupManager _SaveManager = new BackupManager();

        public IAsyncRelayCommand SaveCommand { get; }

        public MainWindow()
        {
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        private async Task SaveAsync()
        {
            bool value = await _SaveManager.SaveAsync();

        }
    }
}
