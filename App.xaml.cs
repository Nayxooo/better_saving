using System.Windows;

namespace better_saving
{
    public partial class App : System.Windows.Application {

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
}
