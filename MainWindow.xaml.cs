using System.Windows;

namespace SimpleWpfApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MyTextBlock.Text = "Tu as cliqué !";
        }
    }
}
