using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySave.ViewModels; // Added for MainViewModel
using EasySave.Models;     // Added for JobType, JobState if needed directly by View for enums (e.g. in forms)

public enum ConsoleMenu
{
    MainMenu,
    Language,
    CreateJob,
    ShowJob,
    StartJob,
    StopJob,
    DeleteJob,
    Exit
}

public class ConsoleInterface
{
    private static MainViewModel? _mainViewModel; // Added MainViewModel instance
    // Language setting (default: English)
    private static string Language = "en";
    // Current menu state
#pragma warning disable CS0414 // Variable is assigned but never used
    private static ConsoleMenu currentMenu = ConsoleMenu.MainMenu;
#pragma warning restore CS0414
    // Dictionary for UI text in different languages
    private static Dictionary<string, Dictionary<string, string>> uiText = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Initializes the console interface with the provided MainViewModel and sets up language configurations.
    /// </summary>
    /// <param name="mainViewModel">The MainViewModel instance to be used for data and operations.</param>
    public static void Initialize(MainViewModel mainViewModel) // Modified parameter
    {
        _mainViewModel = mainViewModel; // Store the ViewModel instance
        InitializeLanguages();
        // Ensure _mainViewModel is not null before proceeding if critical operations depend on it here
        if (_mainViewModel == null)
        {
            // Handle error: ViewModel not provided
            MessageBox.ErrorQuery("Error", "MainViewModel not initialized.", "Ok");
            Application.RequestStop();
            return;
        }
    }

    /// <summary>
    /// Initializes the language dictionaries used for UI text localization.
    /// Sets up English and French language options.
    /// </summary>
    private static void InitializeLanguages()
    {
        // English text
        var englishText = new Dictionary<string, string>
        {
            { "title", "EasySave" },
            { "mainMenu", "Main Menu" },
            { "createJob", "Create New Backup Job" },
            { "viewJobs", "View Jobs" },
            { "startJob", "Start Job" },
            { "stopJob", "Stop Job" },
            { "deleteJob", "Delete Job" },
            { "startAllJobs", "Start All Jobs" },
            { "language", "Change Language" },
            { "exit", "Exit" },
            { "selectLanguage", "Select Language:" },
            { "english", "English" },
            { "french", "French" },
            { "jobName", "Job Name:" },
            { "sourceDir", "Source Directory:" },
            { "targetDir", "Target Directory:" },
            { "jobType", "Job Type:" },
            { "fullBackup", "Full Backup" },
            { "diffBackup", "Differential Backup" },
            { "create", "Create" },
            { "cancel", "Cancel" },
            { "back", "Back" },
            { "status", "Status: " },
            { "progress", "Progress: " },
            { "start", "Start" },
            { "stop", "Stop" },
            { "delete", "Delete" },
            { "confirm", "Confirm" },
            { "confirmDelete", "Are you sure you want to delete this job?" },
            { "help", "Help" },
            { "menu", "Menu" },
            { "quit", "Quit" },
            { "info", "Info" },
            { "error", "Error" },
            { "success", "Success" },
            { "noJobsFound", "No jobs found" },
            { "noJobsToStart", "No jobs found to start" },
            { "allJobsStarted", "All jobs have been started" },
            { "jobNotFound", "Job '{0}' not found" },
            { "jobStopping", "Job '{0}' is stopping..." },
            { "jobStarted", "Job '{0}' started" },
            { "jobDeletedSuccess", "Job '{0}' was deleted successfully." },
            { "failedStartJobs", "Failed to start all jobs: {0}" },
            { "failedStartJob", "Failed to start job: {0}" },
            { "failedStopJob", "Failed to stop job: {0}" },
            { "allFieldsRequired", "All fields are required" },
            { "jobCreateSuccess", "Job created successfully" },
            { "directoryError", "Directory Error" },
            { "ioError", "I/O Error" },
            { "jobError", "Job Error" },
            { "unexpectedError", "Unexpected Error" },
            { "failedCreateJob", "Failed to create job: {0}\n\nDetails: {1}" },
            { "exitConfirm", "Are you sure you want to exit?" },
            { "yes", "yes" },
            { "no", "no" },
            { "ok", "OK" },
            { "latestError", "Latest error:" },
            { "jobsListInstructions", "Use arrow keys to select a job, then press Enter to view details" },
            { "jobDeletedError", "Could not delete job '{0}'. It may be running or no longer exists." },
            { "colName", "Name" },
            { "colType", "Type" },
            { "colState", "State" }, 
            { "colProgress", "Progress" },
            { "jobDetails", "JOB DETAILS: {0}" }
        };

        // French text
        var frenchText = new Dictionary<string, string>
        {
            { "title", "EasySave" },
            { "mainMenu", "Menu Principal" },
            { "createJob", "Créer une Nouvelle Tâche" },
            { "viewJobs", "Voir les Tâches" },
            { "startJob", "Démarrer la Tâche" },
            { "stopJob", "Arrêter la Tâche" },
            { "deleteJob", "Supprimer la Tâche" },
            { "startAllJobs", "Démarrer Toutes les Tâches" },
            { "language", "Changer de Langue" },
            { "exit", "Quitter" },
            { "selectLanguage", "Sélectionner la Langue:" },
            { "english", "Anglais" },
            { "french", "Français" },
            { "jobName", "Nom de la Tâche:" },
            { "sourceDir", "Répertoire Source:" },
            { "targetDir", "Répertoire Cible:" },
            { "jobType", "Type de Tâche:" },
            { "fullBackup", "Sauvegarde Complète" },
            { "diffBackup", "Sauvegarde Différentielle" },
            { "create", "Créer" },
            { "cancel", "Annuler" },
            { "back", "Retour" },
            { "status", "Statut: " },
            { "progress", "Progression: " },
            { "start", "Démarrer" },
            { "stop", "Arrêter" },
            { "delete", "Supprimer" },
            { "confirm", "Confirmer" },
            { "confirmDelete", "Êtes-vous sûr de vouloir supprimer cette tâche?" },
            { "help", "Aide" },
            { "menu", "Menu" },
            { "quit", "Quitter" },
            { "info", "Information" },
            { "error", "Erreur" },
            { "success", "Succès" },
            { "noJobsFound", "Aucune tâche trouvée" },
            { "noJobsToStart", "Aucune tâche à démarrer" },
            { "allJobsStarted", "Toutes les tâches ont été démarrées" },
            { "jobNotFound", "Tâche '{0}' introuvable" },
            { "jobStopping", "La tâche '{0}' est en cours d'arrêt..." },
            { "jobStarted", "Tâche '{0}' démarrée" },
            { "jobDeletedSuccess", "La tâche '{0}' a été supprimée avec succès." },
            { "failedStartJobs", "Échec du démarrage de toutes les tâches: {0}" },
            { "failedStartJob", "Échec du démarrage de la tâche: {0}" },
            { "failedStopJob", "Échec de l'arrêt de la tâche: {0}" },
            { "allFieldsRequired", "Tous les champs sont obligatoires" },
            { "jobCreateSuccess", "Tâche créée avec succès" },
            { "directoryError", "Erreur de Répertoire" },
            { "ioError", "Erreur d'E/S" },
            { "jobError", "Erreur de Tâche" },
            { "unexpectedError", "Erreur Inattendue" },
            { "failedCreateJob", "Échec de la création de la tâche: {0}\n\nDétails: {1}" },
            { "exitConfirm", "Êtes-vous sûr de vouloir quitter?" },
            { "yes", "oui" },
            { "no", "non" },
            { "ok", "OK" },
            { "latestError", "Dernière erreur:" },
            { "jobsListInstructions", "Utilisez les flèches pour sélectionner une tâche, puis appuyez sur Entrée pour voir les détails" },
            { "jobDeletedError", "Impossible de supprimer la tâche '{0}'. Elle peut être en cours d'exécution ou n'existe plus." },
            { "colName", "Nom" },
            { "colType", "Type" },
            { "colState", "État" }, 
            { "colProgress", "Progression" },
            { "jobDetails", "DÉTAILS DE LA TÂCHE: {0}" }
        };

        uiText.Add("en", englishText);
        uiText.Add("fr", frenchText);
    }

    /// <summary>
    /// Starts the terminal GUI application with a dark theme and displays the main menu.
    /// Sets up the application's top-level window, menu, and status bar.
    /// </summary>
    public static void Start()
    {
        Application.Init();
        
        // Set dark theme colors
        Colors.Base.Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black);
        Colors.Base.Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
        Colors.Base.HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black);
        Colors.Base.HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray);
        
        Colors.Dialog.Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
        Colors.Dialog.Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
        Colors.Dialog.HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray);
        Colors.Dialog.HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Gray);
        
        Colors.Error.Normal = Application.Driver.MakeAttribute(Color.Red, Color.Black);
        Colors.Error.Focus = Application.Driver.MakeAttribute(Color.Black, Color.Red);
        Colors.Error.HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black);
        Colors.Error.HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightRed);
        
        Colors.Menu.Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
        Colors.Menu.Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
        Colors.Menu.HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray);
        Colors.Menu.HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Gray);
        
        var top = Application.Top;
        
        // Set current menu to main menu
        currentMenu = ConsoleMenu.MainMenu;
        
        // Create status bar that shows current menu
        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.F1, $"~F1~ {GetText("help")}", null),
            new StatusItem(Key.F10, $"~F10~ {GetText("quit")}", () => RequestExit())
        });
        
        top.Add(statusBar);
        
        // Show main menu on start
        ShowMainMenu();
        
        Application.Run();
        Application.Shutdown();
    }

    /// <summary>
    /// Retrieves the localized text for a given key from the current language dictionary.
    /// </summary>
    /// <param name="key">The key to look up in the language dictionary.</param>
    /// <returns>The localized text string, or the key itself if not found.</returns>
    private static string GetText(string key)
    {
        if (uiText.ContainsKey(Language) && uiText[Language].ContainsKey(key))
        {
            return uiText[Language][key];
        }
        return key; // Return key if text not found
    }

    /// <summary>
    /// Displays the main menu with options to create, view, and manage backup jobs.
    /// Sets up the main window with a title, buttons, and menu bar.
    /// </summary>
    private static void ShowMainMenu()
    {
        var top = Application.Top;

        // Declare menuBar (named 'menu' here) at the beginning
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem (GetText("exit"), "", () => RequestExit(), null, null, Key.Q | Key.CtrlMask)
            }),
            // Added Language to the Options menu for better organization
            new MenuBarItem ("_Options", new MenuItem [] {
                new MenuItem (GetText("language"), "", () => ShowLanguageSelectionDialog())
            })
        });

        // Clear existing content window (if any) before adding new ones
        // Iterate backwards to safely remove items from the collection
        for (int i = top.Subviews.Count - 1; i >= 0; i--)
        {
            var subView = top.Subviews[i];
            // Only remove Windows that are not the menu itself
            if (subView is Window && subView != menu) 
            {
                top.Remove(subView);
                subView.Dispose();
            }
        }

        var window = new Window(GetText("mainMenu"))
        {
            X = 0,
            Y = 1, // Position below the MenuBar
            Width = Dim.Fill(),
            Height = Dim.Fill() -1 // Adjust for status bar, assuming status bar is 1 line
        };

        var createJobButton = new Button(GetText("createJob"))
        {
            X = Pos.Center(),
            Y = Pos.Center() - 4,
        };
        createJobButton.Clicked += () => {
            ShowCreateJobForm(); 
        };

        var showJobsButton = new Button(GetText("viewJobs"))
        {
            X = Pos.Center(),
            Y = Pos.Center() - 2,
        };
        showJobsButton.Clicked += () => {
            ShowJobsList();
        };

        var startAllJobsButton = new Button(GetText("startAllJobs"))
        {
            X = Pos.Center(),
            Y = Pos.Center(),
        };
        startAllJobsButton.Clicked += () => {
            // Ensure _mainViewModel and its command are available
            if (_mainViewModel?.StartAllJobsCommand?.CanExecute(null) ?? false)
            {
                _mainViewModel.StartAllJobsCommand.Execute(null);
                // Provide feedback. This might be better handled by observing command completion/status.
                MessageBox.Query(GetText("info"), GetText("allJobsStarted"), GetText("ok"));
            }
            else
            {
                MessageBox.ErrorQuery(GetText("info"), GetText("noJobsToStart"), GetText("ok"));
            }
        };

        var changeLanguageButton = new Button(GetText("language"))
        {
            X = Pos.Center(),
            Y = Pos.Center() + 2,
        };
        changeLanguageButton.Clicked += () => {
            ShowLanguageSelectionDialog();
        };

        var exitButton = new Button(GetText("exit"))
        {
            X = Pos.Center(),
            Y = Pos.Center() + 4,
        };
        exitButton.Clicked += () => {
            RequestExit();
        };
        
        window.Add(createJobButton, showJobsButton, startAllJobsButton, changeLanguageButton, exitButton);
        
        // Add menu and window to the top-level view
        // Add menu bar if it's not already there
        if (!top.Subviews.Contains(menu))
        {
            top.Add(menu);
        }
        top.Add(window); // Add the main content window

        // Set focus
        window.FocusFirst(); 
        if (!window.HasFocus) { 
            window.SetFocus();
        }
    }
    
    private static void ShowCreateJobForm()
    {
        MessageBox.Query("Info", "Create Job form not fully implemented in this refactoring step.", "Ok");
    }

    private static void ShowJobsList()
    {
        MessageBox.Query("Info", "Show Jobs list not fully implemented in this refactoring step.", "Ok");
    }

    private static void ShowLanguageSelectionDialog()
    {
        var dialog = new Dialog(GetText("selectLanguage"), 60, 10);
        var englishButton = new Button(GetText("english")) { X = Pos.Center() - 10, Y = Pos.Center() - 1 };
        var frenchButton = new Button(GetText("french")) { X = Pos.Center() + 10, Y = Pos.Center() - 1 };

        englishButton.Clicked += () => {
            Language = "en";
            Application.RequestStop(dialog); 
            ShowMainMenu(); 
        };
        frenchButton.Clicked += () => {
            Language = "fr";
            Application.RequestStop(dialog); 
            ShowMainMenu(); 
        };

        var cancelButton = new Button(GetText("cancel"), is_default: true);
        cancelButton.Clicked += () => Application.RequestStop(dialog);
        dialog.AddButton(cancelButton);

        dialog.Add(englishButton, frenchButton);
        Application.Run(dialog);
    }

    /// <summary>
    /// Shows a confirmation dialog for exiting the application.
    /// </summary>
    private static void RequestExit()
    {
        var n = MessageBox.Query(50, 7, GetText("exit"), GetText("exitConfirm"), GetText("no"), GetText("yes"));
        if (n == 1)
        {
            Application.RequestStop();
        }
    }
}
