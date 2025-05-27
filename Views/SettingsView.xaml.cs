using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace better_saving.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void MaxFileTransferSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = MyRegex(); // Regex to allow only numbers
            e.Handled = regex.IsMatch(e.Text);
        }

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex MyRegex();
    }
}