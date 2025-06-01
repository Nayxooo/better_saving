using System.Windows;
using System.Windows.Threading; // Required for DispatcherUnhandledExceptionEventArgs
using better_saving.Models; // Required for Logger
using System.IO; // Required for Path

namespace better_saving
{
    public partial class App : System.Windows.Application {

        private Logger? _appLogger;
        private readonly string errorLogPath = Path.Combine(AppContext.BaseDirectory, "logs\\EasySave33.bugReport");
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure only one instance of the application runs
            var _appMutex = new Mutex(true, "EasySave33Mutex", out bool isNewInstance);
            if (!isNewInstance)
            {
                // If another instance is already running, show a message and exit
                System.Windows.MessageBox.Show("Another instance of EasySave33 is already running.", "Instance Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(0);
            }

            // Lower-level exception handler
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);

            // Initialize logger early
            try
            {
                _appLogger = new Logger();
                // It's good practice to set the job provider if your logger needs it for some operations,
                // but for basic error logging at startup, it might not be strictly necessary yet.
                // If MainViewModel or BackupListViewModel is available as a resource, you could try to get it here.
                // For now, we'll assume basic logging doesn't require the job list.
            }
            catch (Exception ex)
            {
                // Fallback if logger instantiation fails - perhaps write to a simple text file or event log
                try
                {
                    string errorText = $"{DateTime.Now}: Logger initialization failed: {ex}\n";
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "logs\\EasySave33.bugReport"), $"\"{errorText}\"");
                }
                catch { /* Swallow exception if fallback logging fails */ }
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
                    errorMessage += $"Exception: {ex.ToString()}\n";
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
                // Fallback if logging itself fails
                try
                {
                    File.AppendAllText(errorLogPath, $"{DateTime.Now}: Logging domain exception failed: {logEx}\nOriginal exception: {e.ExceptionObject?.ToString() ?? "null"}\n");
                }
                catch { /* Ultimate fallback: swallow if even this fails */ }
            }

            // Optionally, inform the user if possible and not terminating
            // System.Windows.MessageBox.Show("A critical error occurred. The application might close. Check critical_domain_error.txt.", "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            // If e.IsTerminating is true, the application will close anyway.
            // If it's false, you might decide to try and continue or gracefully exit.
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _appLogger?.LogError($"ApplicationStartupError | DispatcherUnhandledException \"{e.Exception}\"");
            // Optionally, display a message to the user
            System.Windows.MessageBox.Show("An unexpected error occurred. Please check the logs for more details.", "Application Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            
            // Prevent application from crashing
            e.Handled = true; 
            // Environment.Exit(1); // Or, if you want to force exit
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
}
