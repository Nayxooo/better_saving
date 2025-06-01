using System.Windows;
using better_saving.Models;

namespace better_saving
{
    public partial class MainWindow : Window
    {        public MainWindow()
        {
            InitializeComponent();
            // Assign our MainViewModel as the DataContext
            DataContext = new ViewModels.MainViewModel();
        }
    }
}
