using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace better_saving;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "better_saving_unique_instance";
        bool createdNew;

        _mutex = new Mutex(true, mutexName, out createdNew);

        if (!createdNew)
        {
            System.Windows.MessageBox.Show("L'application est déjà en cours d'exécution.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(); // ferme immédiatement
            return;
        }

        base.OnStartup(e); // continue l'exécution normale (ouvre MainWindow)
    }
    public static void LoadLanguageDictionary(string cultureName)
    {
        // Chemin vers les fichiers : “Resources/Localization/Strings.<culture>.xaml”
        var dictPath = $"/Resources/Localization/Strings.{cultureName}.xaml";
        var dict = new ResourceDictionary { Source = new Uri(dictPath, UriKind.Relative) };

        // Retire l’ancien dictionnaire s’il existe
        var old = Current.Resources.MergedDictionaries
                   .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") == true);
        if (old != null) Current.Resources.MergedDictionaries.Remove(old);

        Current.Resources.MergedDictionaries.Add(dict);
    }
}
