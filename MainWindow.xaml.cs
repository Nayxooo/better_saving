using System.Threading.Tasks;
using System.Windows.Input;
using better_saving;

namespace better_saving.ViewModels
{
    public class MainViewModel : BaseViewModel // ou INotifyPropertyChanged
    {
        private readonly SaveManager _saveManager = new SaveManager();

        public ICommand SaveCommand { get; }

        public MainViewModel()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }

        private async Task SaveAsync()
        {
            await _saveManager.SaveAsync();
        }
    }
}
