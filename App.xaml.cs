using System.Windows;
using System.Windows.Threading; // Required for DispatcherUnhandledExceptionEventArgs
using better_saving.Models; // Required for Logger
using System.IO; // Required for Path
using System.Linq; // Required for FirstOrDefault
using System; // Required for AppDomain
using System.Threading; // ✅ Pour Mutex

namespace better_saving
{
    public partial class App : System.Windows.Application
    {
        private Logger? _appLogger;

        private static Mutex? _appMutex; // ✅ Mutex déclaré ici

        private readonly string errorLogPath = Path.Combine(AppContext.BaseDirectory, "logs\\EasySave33.bugReport");

        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ Gestion de l'instance unique
            const string mutexName = "BETTER_SAVING_INSTANCE_UNIQUE";
            bool isNewInstance;
            _appMutex = new Mutex(true, mutexName, out isNewInstance);

            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("L'application est déjà en cours d'exécution.", "Instance détectée", MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(0); // Quitte proprement
                return;
            }

            // Lower-level exception handler
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);

            // Initialize logger early
            try
            {
                _appLogger = new Logger();
            }
            catch (Exception ex)
            {
                try
                {
                    string errorText = $"{DateTime.Now}: Logger initialization failed: {ex}\n";
                    File.AppendAllText(errorLogPath, $"\"{errorText}\"");
                }
                catch { }
            }

            // Global exception handler
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMessage = "\nAn unhandled domain exception occurred.\n";
                if (e.ExceptionObject is Exception ex)
                {
                    errorMessage += $"Exception: {ex}\n";
                }
                else
                {
                    errorMessage += $"Exception object: {e.ExceptionObject?.ToString() ?? "null"}\n";
                }
                errorMessage += $"IsTerminating: {e.IsTerminating}\n";

                _appLogger?.LogError($"ApplicationStartupError | AppDomainUnhandledException \"{errorMessage}\"");
            }
            catch (Exception logEx)
            {
                try
                {
                    File.AppendAllText(errorLogPath, $"{DateTime.Now}: Logging domain exception failed: {logEx}\nOriginal exception: {e.ExceptionObject?.ToString() ?? "null"}\n");
                }
                catch { }
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _appLogger?.LogError($"ApplicationStartupError | DispatcherUnhandledException \"{e.Exception}\"");
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("An unexpected error occurred. Please check the logs for more details.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        public static void LoadLanguageDictionary(string cultureName)
        {
            var dictPath = $"/Resources/Localization/Strings.{cultureName}.xaml";
            var dict = new ResourceDictionary { Source = new Uri(dictPath, UriKind.Relative) };

            var old = Current.Resources.MergedDictionaries
                       .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") == true);
            if (old != null) Current.Resources.MergedDictionaries.Remove(old);

            Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
