using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    // Dictionary to store created jobs
    private static Dictionary<string, backupJob> jobs = [];
    // Language setting (default: English)
    private static string Language = "en";
    // Logger instance - make nullable to avoid non-null issues
    private static Logger? logger;
    // Current menu state
#pragma warning disable CS0414 // Variable is assigned but never used
    private static ConsoleMenu currentMenu = ConsoleMenu.MainMenu;
#pragma warning restore CS0414
    // Dictionary for UI text in different languages
    private static Dictionary<string, Dictionary<string, string>> uiText = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Initializes the console interface with the provided logger and sets up language configurations.
    /// </summary>
    /// <param name="applicationLogger">The logger instance to be used for logging operations.</param>
    public static void Initialize(Logger applicationLogger)
    {
        logger = applicationLogger;
        InitializeLanguages();
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
        
        // Create top-level window (explicitly using the menu title based on current menu)
        var win = new Window(GetMenuTitle())
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.DarkGray
            }
        };
        
        // Create status bar that shows current menu
        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.F1, $"~F1~ {GetText("help")}", null),
            new StatusItem(Key.F2, $"~F2~ {GetText("menu")}", () => ShowMainMenu()),
            new StatusItem(Key.F10, $"~F10~ {GetText("quit")}", () => RequestExit())
        });
        
        top.Add(win, statusBar);
        
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
        currentMenu = ConsoleMenu.MainMenu;
        Application.Top.RemoveAll();
        
        var top = Application.Top;
        
        var win = new Window(GetText("mainMenu"))
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.DarkGray
            }
        };
        
        // Add a title with styling
        var titleLabel = new Label(GetText("title").ToUpper())
        {
            X = Pos.Center(),
            Y = 2,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
            }
        };
        
        // Create a frame for the buttons
        var buttonFrame = new FrameView()
        {
            X = Pos.Center() - 20,
            Y = 5,
            Width = 40,
            Height = 16, // Increased height to accommodate the new button
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.Gray
            }
        };

        var createBtn = new Button(GetText("createJob"))
        {
            X = Pos.Center(),
            Y = 2,
            Width = 20,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
            }
        };
        createBtn.Clicked += () => CreateJobDialog();
        
        var viewBtn = new Button(GetText("viewJobs"))
        {
            X = Pos.Center(),
            Y = 4,
            Width = 20,
            ColorScheme = createBtn.ColorScheme
        };
        viewBtn.Clicked += () => ListJobsDialog();
        
        var startAllBtn = new Button(GetText("startAllJobs"))
        {
            X = Pos.Center(),
            Y = 6,
            Width = 20,
            ColorScheme = createBtn.ColorScheme
        };
        startAllBtn.Clicked += () => {
            try
            {
                var jobs = Controller.RetrieveBackupJobs();
                if (jobs.Count == 0)
                {
                    MessageBox.Query(60, 7, GetText("info"), GetText("noJobsToStart"), GetText("ok"));
                    return;
                }
                
                Controller.StartAllJobs();
                MessageBox.Query(60, 7, GetText("info"), GetText("allJobsStarted"), GetText("ok"));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery(60, 8, GetText("error"), string.Format(GetText("failedStartJobs"), ex.Message), GetText("ok"));
            }
        };
        
        var langBtn = new Button(GetText("language"))
        {
            X = Pos.Center(),
            Y = 8,
            Width = 20,
            ColorScheme = createBtn.ColorScheme
        };
        langBtn.Clicked += () => SetLanguageDialog();
        
        var exitBtn = new Button(GetText("exit"))
        {
            X = Pos.Center(),
            Y = 10,
            Width = 20,
            ColorScheme = createBtn.ColorScheme
        };
        exitBtn.Clicked += () => RequestExit();
        
        buttonFrame.Add(createBtn, viewBtn, startAllBtn, langBtn, exitBtn);
        
        // Add version info at the bottom
        var versionLabel = new Label("v1.0.0")
        {
            X = Pos.Right(win) - 8,
            Y = Pos.Bottom(win) - 2,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black)
            }
        };
        
        win.Add(titleLabel, buttonFrame, versionLabel);
        
        var menu = new MenuBar(new MenuBarItem[] {
            new(GetText("mainMenu"), [
                new(GetText("createJob"), "", () => CreateJobDialog()),
                new(GetText("viewJobs"), "", () => ListJobsDialog()),
                new(GetText("startAllJobs"), "", () => {
                    try
                    {
                        var jobs = Controller.RetrieveBackupJobs();
                        if (jobs.Count == 0)
                        {
                            MessageBox.Query(60, 7, GetText("info"), GetText("noJobsToStart"), GetText("ok"));
                            return;
                        }
                        
                        Controller.StartAllJobs();
                        MessageBox.Query(60, 7, GetText("info"), GetText("allJobsStarted"), GetText("ok"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery(60, 8, GetText("error"), string.Format(GetText("failedStartJobs"), ex.Message), GetText("ok"));
                    }
                }),
                new MenuItem(GetText("language"), "", () => SetLanguageDialog()),
                new MenuItem(GetText("exit"), "", () => RequestExit())
            ])
        });
        
        var statusBar = new StatusBar([
            new(Key.F1, $"~F1~ {GetText("help")}", null),
            new(Key.F10, $"~F10~ {GetText("quit")}", () => RequestExit())
        ]);
        
        top.Add(win, menu, statusBar);
        // Set initial focus on the first button
        createBtn.SetFocus();
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
    
    /// <summary>
    /// Creates a text-based progress bar showing completion percentage and file count.
    /// </summary>
    /// <param name="progress">The percentage of completion (0-100).</param>
    /// <param name="totalFiles">The total number of files to process.</param>
    /// <param name="remainingFiles">The number of files remaining to be processed.</param>
    /// <returns>A formatted string representing the progress bar with completion percentage and file count.</returns>
    private static string GetProgressBar(int progress, int totalFiles, int remainingFiles)
    {
        // Create a progress bar string based on the progress percentage
        int barLength = 40; // Shortened to make room for file count
        int filledLength = (int)(barLength * progress / 100);
        string bar = new string('█', filledLength) + new string(' ', barLength - filledLength);
        int transferredFiles = totalFiles - remainingFiles;
        return $"[{bar}] {progress}%   ({transferredFiles}/{totalFiles})";
    }

    /// <summary>
    /// Displays a dialog for selecting the application language.
    /// Allows users to switch between English and French.
    /// </summary>
    private static void SetLanguageDialog()
    {
        currentMenu = ConsoleMenu.Language;
        
        var dialog = new Dialog(GetText("selectLanguage"), 50, 10);
        
        var cancelBtn = new Button(GetText("cancel"))
        {
            X = 1,
            Y = 5
        };
        cancelBtn.Clicked += () => {
            dialog.RequestStop();
        };
        
        var englishBtn = new Button(GetText("english"))
        {
            X = Pos.Right(cancelBtn) + 2,
            Y = 1
        };
        englishBtn.Clicked += () => {
            Language = "en";
            dialog.RequestStop();
            ShowMainMenu(); // Refresh UI with new language
        };
        
        var frenchBtn = new Button(GetText("french"))
        {
            X = Pos.Right(cancelBtn) + 2,
            Y = 3
        };
        frenchBtn.Clicked += () => {
            Language = "fr";
            dialog.RequestStop();
            ShowMainMenu(); // Refresh UI with new language
        };
        
        dialog.Add(cancelBtn, englishBtn, frenchBtn);
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Displays a dialog for creating a new backup job.
    /// Collects job name, source directory, target directory, and job type from the user.
    /// </summary>
    private static void CreateJobDialog()
    {
        currentMenu = ConsoleMenu.CreateJob;
        
        // Increase dialog width to accommodate long path names
        var dialog = new Dialog(GetText("createJob"), 100, 20)
        {
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.Gray
            },
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray)
            }
        };
        
        // Title with styling
        var titleLabel = new Label(GetText("createJob").ToUpper())
        {
            X = Pos.Center(),
            Y = 0,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
            }
        };
        
        var nameLabel = new Label(GetText("jobName"))
        {
            X = 2,
            Y = 2,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
            }
        };
        
        // Increased width for the text fields to show longer paths
        var nameField = new TextField("")
        {
            X = 22,
            Y = 2,
            Width = 70,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
                Focus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Blue)
            }
        };
        
        var sourceLabel = new Label(GetText("sourceDir"))
        {
            X = 2,
            Y = 4,
            ColorScheme = nameLabel.ColorScheme
        };
        
        var sourceField = new TextField("")
        {
            X = 22,
            Y = 4,
            Width = 70,
            ColorScheme = nameField.ColorScheme
        };
        
        var targetLabel = new Label(GetText("targetDir"))
        {
            X = 2,
            Y = 6,
            ColorScheme = nameLabel.ColorScheme
        };
        
        var targetField = new TextField("")
        {
            X = 22,
            Y = 6,
            Width = 70,
            ColorScheme = nameField.ColorScheme
        };
        
        var typeLabel = new Label(GetText("jobType"))
        {
            X = 2,
            Y = 8,
            ColorScheme = nameLabel.ColorScheme
        };
        
        // Fix RadioGroup constructor - convert strings to NStack.ustring and use proper Rect
        var radioLabels = new[] { GetText("fullBackup"), GetText("diffBackup") }.Select(x => (NStack.ustring)x).ToArray();
        var radioGroup = new RadioGroup(new Rect(22, 8, 70, 2), radioLabels)
        {
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
            }
        };
        
        // Add a separator line - increased length to match wider dialog
        var separator = new Label(new string('─', 96))
        {
            X = 1,
            Y = 11,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black)
            }
        };
        
        // Cancel button on the left
        var cancelBtn = new Button(GetText("cancel"))
        {
            X = 22,
            Y = 13,
            Width = 15,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Blue),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Blue)
            }
        };
        
        cancelBtn.Clicked += () => {
            dialog.RequestStop();
        };
        
        // Create button on the right
        var createBtn = new Button(GetText("create"))
        {
            X = 77, // Position it on the right side
            Y = 13,
            Width = 15,
            ColorScheme = cancelBtn.ColorScheme
        };
        
        createBtn.Clicked += () => {
            if (string.IsNullOrEmpty(nameField.Text?.ToString()) || 
                string.IsNullOrEmpty(sourceField.Text?.ToString()) || 
                string.IsNullOrEmpty(targetField.Text?.ToString()))
            {
                MessageBox.ErrorQuery(50, 7, GetText("error"), GetText("allFieldsRequired"), GetText("ok"));
                return;
            }
            
            string name = nameField.Text?.ToString() ?? "";
            string sourceDir = NormalizePath(sourceField.Text?.ToString() ?? "");
            string targetDir = NormalizePath(targetField.Text?.ToString() ?? "");
            
            JobType type = radioGroup.SelectedItem == 0 ? JobType.Full : JobType.Diff;
            
            try
            {
                Controller.CreateBackupJob(name, sourceDir, targetDir, type);
                
                dialog.RequestStop();
                MessageBox.Query(50, 7, GetText("success"), GetText("jobCreateSuccess"), GetText("ok"));
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.ErrorQuery(60, 8, GetText("directoryError"), ex.Message, GetText("ok"));
            }
            catch (IOException ex)
            {
                MessageBox.ErrorQuery(60, 8, GetText("ioError"), ex.Message, GetText("ok"));
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.ErrorQuery(60, 8, GetText("jobError"), ex.Message, GetText("ok"));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery(60, 10, GetText("unexpectedError"), string.Format(GetText("failedCreateJob"), ex.Message, ex.GetType().Name), GetText("ok"));
            }
        };
        
        dialog.Add(titleLabel, nameLabel, nameField, sourceLabel, sourceField, targetLabel, targetField, 
                 typeLabel, radioGroup, separator, cancelBtn, createBtn);
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Displays a list of all configured backup jobs.
    /// Shows job names, types, states, and progress, and allows selecting a job for detailed view.
    /// </summary>
    private static void ListJobsDialog()
    {
        currentMenu = ConsoleMenu.MainMenu;
        
        // Clear any previous UI elements
        Application.Top.RemoveAll();
        
        var jobs = Controller.RetrieveBackupJobs();
        
        if (jobs.Count == 0)
        {
            MessageBox.Query(50, 7, GetText("info"), GetText("noJobsFound"), GetText("ok"));
            ShowMainMenu(); // Return to main menu if no jobs found
            return;
        }
        
        // Set up the top-level window and status bar
        var top = Application.Top;
        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.F1, $"~F1~ {GetText("help")}", null),
            new StatusItem(Key.F2, $"~F2~ {GetText("menu")}", () => ShowMainMenu()),
            new StatusItem(Key.F10, $"~F10~ {GetText("quit")}", () => RequestExit())
        });
        top.Add(statusBar);
        
        // Increased dialog width to 100 (from 80) for better display of paths
        var dialog = new Dialog(GetText("viewJobs"), 100, 25)
        {
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.Gray
            }
        };
        
        // Title with styling
        var titleLabel = new Label(GetText("viewJobs").ToUpper())
        {
            X = Pos.Center(),
            Y = 0,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
            }
        };
        
        // Create column headers with styling - expanded width for better path display
        var headerLabel = new Label($"{GetText("colName"),-25} {GetText("colType"),-15} {GetText("colState"),-15} {GetText("colProgress"),-15}")
        {
            X = 1,
            Y = 2,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
            }
        };
        
        // Add a separator line - increased length
        var separator = new Label(new string('─', 96))
        {
            X = 1,
            Y = 3,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray)
            }
        };
        
        // Create a list of job information strings formatted as a table - expanded column widths
        var jobInfoList = jobs.Select(job => 
            $"{job.Name,-25} {job.GetJobType(),-15} {job.GetState(),-15} {job.GetProgress(),3}%"
        ).ToList();
        
        var listView = new ListView(jobInfoList)
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 7,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Gray)
            }
        };
        
        // Handle key press events for the list view
        listView.KeyPress += (args) => {
            // When Enter or Space key is pressed and a job is selected
            if ((args.KeyEvent.Key == Key.Enter || args.KeyEvent.Key == Key.Space) && 
                listView.SelectedItem >= 0 && listView.SelectedItem < jobs.Count)
            {
                args.Handled = true;
                dialog.RequestStop();
                ShowJob(jobs[listView.SelectedItem].Name);
            }
        };
        
        // The instruction label is still useful to guide users
        var instructionLabel = new Label(GetText("jobsListInstructions"))
        {
            X = 1,
            Y = Pos.Bottom(listView),
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray)
            }
        };
        
        var backBtn = new Button(GetText("back"))
        {
            X = 1,
            Y = Pos.Bottom(listView) + 1,
            Width = 15,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Gray)
            }
        };
        backBtn.Clicked += () => {
            dialog.RequestStop();
            ShowMainMenu(); // Return to main menu when back is clicked
        };
        
        dialog.Add(titleLabel, headerLabel, separator, listView, instructionLabel, backBtn);
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Displays detailed information about a specific backup job.
    /// Shows job properties, state, progress, and provides options to start, stop, or delete the job.
    /// </summary>
    /// <param name="jobName">The name of the job to display.</param>
    private static void ShowJob(string jobName)
    {
        currentMenu = ConsoleMenu.ShowJob;
        
        // Clear any previous UI elements
        Application.Top.RemoveAll();
        
        var jobs = Controller.RetrieveBackupJobs();
        var job = jobs.FirstOrDefault(j => j.Name == jobName);
        
        if (job == null)
        {
            MessageBox.ErrorQuery(50, 7, GetText("error"), string.Format(GetText("jobNotFound"), jobName), GetText("ok"));
            // Go back to job list if job not found
            ListJobsDialog();
            return;
        }
        
        // Set up the top-level window and status bar to ensure consistent UI
        var top = Application.Top;
        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.F1, $"~F1~ {GetText("help")}", null),
            new StatusItem(Key.F2, $"~F2~ {GetText("menu")}", () => ShowMainMenu()),
            new StatusItem(Key.F10, $"~F10~ {GetText("quit")}", () => RequestExit())
        });
        top.Add(statusBar);
        
        var dialog = new Dialog(string.Format(GetText("jobDetails"), job.Name), 150, 30)
        {
            Border = new Border
            {
                BorderStyle = BorderStyle.Rounded,
                BorderBrush = Color.Gray
            }
        };
        
        // Title with styling
        var titleLabel = new Label(string.Format(GetText("jobDetails"), job.Name))
        {
            X = Pos.Center(),
            Y = 0,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
            }
        };
        
        // Get source and target directories with exactly two backslashes
        string sourceDir = job.GetSourceDirectory().Replace("\\", "\\\\");
        string targetDir = job.GetTargetDirectory().Replace("\\", "\\\\");
        
        // Create labels with improved styling
        var fieldColorScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.DarkGray)
        };
        
        var valueColorScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray)
        };
        
        var nameLabel = new Label(GetText("jobName"))
        {
            X = 2,
            Y = 2,
            ColorScheme = fieldColorScheme
        };
        
        var nameValue = new Label(job.Name)
        {
            X = 20,
            Y = 2,
            ColorScheme = valueColorScheme
        };
        
        var sourceLabel = new Label(GetText("sourceDir"))
        {
            X = 2,
            Y = 4,
            ColorScheme = fieldColorScheme
        };
        
        var sourceValue = new Label(sourceDir)
        {
            X = 20,
            Y = 4,
            ColorScheme = valueColorScheme,
            Width = Dim.Fill() - 22
        };
        
        var targetLabel = new Label(GetText("targetDir"))
        {
            X = 2,
            Y = 6,
            ColorScheme = fieldColorScheme
        };
        
        var targetValue = new Label(targetDir)
        {
            X = 20,
            Y = 6,
            ColorScheme = valueColorScheme,
            Width = Dim.Fill() - 22
        };
        
        var typeLabel = new Label(GetText("jobType"))
        {
            X = 2,
            Y = 8,
            ColorScheme = fieldColorScheme
        };
        
        var typeValue = new Label(job.GetJobType().ToString())
        {
            X = 20,
            Y = 8,
            ColorScheme = valueColorScheme
        };
        
        var statusLabel = new Label(GetText("status"))
        {
            X = 2,
            Y = 10,
            ColorScheme = fieldColorScheme
        };
        
        // Display status with color based on state
        var state = job.GetState();
        var statusValue = new Label(state.ToString())
        {
            X = 20,
            Y = 10,
            ColorScheme = new ColorScheme
            {
                Normal = GetStateColor(state)
            }
        };
        
        var progressLabel = new Label(GetText("progress"))
        {
            X = 2,
            Y = 12,
            ColorScheme = fieldColorScheme
        };
        
        // Get progress and file counts directly from job
        int progress = job.GetProgress();
        int totalFiles = job.GetTotalFilesToCopy();
        int remainingFiles = job.GetNumberFilesLeftToDo();
        
        var progressBar = new Label(GetProgressBar(progress, totalFiles, remainingFiles))
        {
            X = 20,
            Y = 12,
            ColorScheme = valueColorScheme
        };
        
        // Get and display error message if any
        string errorMessage = job.GetErrorMessage();
        var errorLabel = new Label(GetText("latestError"))
        {
            X = 2, 
            Y = 14,  // Moved down to have more space
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray)
            },
            Visible = !string.IsNullOrEmpty(errorMessage)
        };
        
        // Display error message as a normal label instead of TextView
        var errorValue = new Label(errorMessage ?? "")
        {
            X = 20,
            Y = 14,  // Moved down to have more space
            Width = Dim.Fill() - 22,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray)
            },
            Visible = !string.IsNullOrEmpty(errorMessage)
        };
        
        // Add a separator line - increased length
        var separator = new Label(new string('─', 116)) // Increased to match wider dialog
        {
            X = 1,
            Y = 20, // Moved down to accommodate multi-line error message
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray)
            }
        };
        
        var backBtn = new Button(GetText("back"))
        {
            X = 2,
            Y = 22, // Moved down accordingly
            Width = 15,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Gray)
            }
        };
        
        backBtn.Clicked += () => {
            dialog.RequestStop();
            Application.MainLoop.AddIdle(() => {
                ListJobsDialog();
                return false;
            });
        };
        
        var buttonColorScheme = backBtn.ColorScheme;
        
        // Check if the job is running to determine which button to show
        bool isJobRunning = Controller.IsJobRunning(job.Name);
        
        Button? actionButton;
        if (isJobRunning) {
            // Add stop button for running jobs
            actionButton = new Button(GetText("stop"))
            {
                X = Pos.Right(backBtn) + 2,
                Y = 22, // Moved down accordingly
                Width = 15,
                ColorScheme = buttonColorScheme
            };
            actionButton.Clicked += () => {
                try
                {
                    Controller.StopJob(job.Name);
                    MessageBox.Query(50, 7, GetText("info"), string.Format(GetText("jobStopping"), job.Name), GetText("ok"));
                    
                    // Refresh job state and progress
                    dialog.RequestStop();
                    Application.MainLoop.AddIdle(() => {
                        ShowJob(job.Name);
                        return false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery(50, 7, GetText("error"), string.Format(GetText("failedStopJob"), ex.Message), GetText("ok"));
                }
            };
            
            // Add only the back and action buttons when job is running
            dialog.Add(
                titleLabel, nameLabel, nameValue, sourceLabel, sourceValue, 
                targetLabel, targetValue, typeLabel, typeValue,
                statusLabel, statusValue, progressLabel, progressBar,
                errorLabel, errorValue,
                separator, backBtn, actionButton
            );
        } else {
            // Add start button for idle jobs
            actionButton = new Button(GetText("start"))
            {
                X = Pos.Right(backBtn) + 2,
                Y = 22, // Moved down accordingly
                Width = 15,
                ColorScheme = buttonColorScheme
            };
            actionButton.Clicked += () => {
                try
                {
                    Controller.StartJob(job.Name);
                    MessageBox.Query(50, 7, GetText("info"), string.Format(GetText("jobStarted"), job.Name), GetText("ok"));
                    
                    // Refresh job state and progress
                    dialog.RequestStop();
                    Application.MainLoop.AddIdle(() => {
                        ShowJob(job.Name);
                        return false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery(50, 7, GetText("error"), string.Format(GetText("failedStartJob"), ex.Message), GetText("ok"));
                }
            };
            
            // Add delete button (only when job is not running)
            var deleteBtn = new Button(GetText("delete"))
            {
                X = Pos.Right(actionButton) + 2,
                Y = 22, // Same level as other buttons
                Width = 15,
                ColorScheme = buttonColorScheme
            };
            
            deleteBtn.Clicked += () => {
                // Confirm deletion with cancel on left, confirm on right
                int result = MessageBox.Query(
                    50, 
                    7, 
                    GetText("deleteJob"), 
                    GetText("confirmDelete"), 
                    GetText("cancel"),
                    GetText("confirm")
                );
                
                if (result == 1) // User confirmed deletion (index 1)
                {
                    try
                    {
                        bool deleted = Controller.DeleteBackupJob(job.Name);
                        if (deleted)
                        {
                            MessageBox.Query(50, 7, GetText("info"), string.Format(GetText("jobDeletedSuccess"), job.Name), GetText("ok"));
                            dialog.RequestStop();
                            
                            // This will properly navigate back to the UI
                            Application.MainLoop.AddIdle(() => {
                                if (Controller.RetrieveBackupJobs().Count > 0)
                                {
                                    ListJobsDialog();
                                }
                                else
                                {
                                    ShowMainMenu();
                                }
                                return false;
                            });
                        }
                        else
                        {
                            // This will happen if the job is running (although the button should be disabled)
                            MessageBox.ErrorQuery(60, 8, GetText("error"), 
                                string.Format(GetText("jobDeletedError"), job.Name), GetText("ok"));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery(60, 8, GetText("error"), string.Format(GetText("failedStopJob"), ex.Message), GetText("ok"));
                    }
                }
            };
            
            // Add all buttons including delete when job is not running
            dialog.Add(
                titleLabel, nameLabel, nameValue, sourceLabel, sourceValue, 
                targetLabel, targetValue, typeLabel, typeValue,
                statusLabel, statusValue, progressLabel, progressBar,
                errorLabel, errorValue,
                separator, backBtn, actionButton, deleteBtn
            );
        }
        
        // Set up a timer to refresh the job status periodically when the job is running
        if (isJobRunning) {
            Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), _ => {
                // Update the status and progress if dialog is still open
                if (!dialog.IsCurrentTop)
                    return false;
                
                var updatedState = job.GetState();
                statusValue.Text = updatedState.ToString();
                statusValue.ColorScheme = new ColorScheme {
                    Normal = GetStateColor(updatedState)
                };
                
                var updatedProgress = job.GetProgress();
                int updatedTotalFiles = job.GetTotalFilesToCopy();
                int updatedRemainingFiles = job.GetNumberFilesLeftToDo();
                progressBar.Text = GetProgressBar(updatedProgress, updatedTotalFiles, updatedRemainingFiles);
                
                // Update error message if any
                string updatedErrorMessage = job.GetErrorMessage();
                errorLabel.Visible = !string.IsNullOrEmpty(updatedErrorMessage);
                
                // Update the error label text
                if (!string.IsNullOrEmpty(updatedErrorMessage))
                {
                    errorValue.Text = updatedErrorMessage;
                    errorValue.Visible = true;
                }
                else
                {
                    errorValue.Visible = false;
                }
                
                return true; // Continue refreshing
            });
        }
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Displays a message box with the specified text.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private static void DisplayMessage(string message)
    {
        MessageBox.Query(50, 7, GetText("info"), message, GetText("ok"));
    }

    /// <summary>
    /// Normalizes a file path by replacing multiple consecutive backslashes with a single one.
    /// </summary>
    /// <param name="path">The path string to normalize.</param>
    /// <returns>The normalized path string.</returns>
    private static string NormalizePath(string path)
    {
        // First replace all backslashes with a single backslash
        string normalizedPath = path.Replace("\\\\", "\\").Replace("\\\\\\", "\\").Replace("\\\\\\\\", "\\");
        
        return normalizedPath;
    }

    /// <summary>
    /// Determines the appropriate color attribute based on the job state.
    /// </summary>
    /// <param name="state">The job state to get a color for.</param>
    /// <returns>A Terminal.Gui Attribute representing the color for the given state.</returns>
    private static Terminal.Gui.Attribute GetStateColor(jobState state)
    {
        return state switch
        {
            jobState.Working => Application.Driver.MakeAttribute(Color.BrightGreen, Color.DarkGray),
            jobState.Finished => Application.Driver.MakeAttribute(Color.Green, Color.DarkGray),
            jobState.Stopped => Application.Driver.MakeAttribute(Color.Brown, Color.DarkGray),
            jobState.Failed => Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray),
            _ => Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray)  // Idle or unknown
        };
    }

    /// <summary>
    /// Checks if the console is currently displaying a specific menu.
    /// </summary>
    /// <param name="menu">The menu to check against the current menu.</param>
    /// <returns>True if the specified menu is currently displayed, false otherwise.</returns>
    private static bool IsInMenu(ConsoleMenu menu)
    {
        return currentMenu == menu;
    }
    
    /// <summary>
    /// Gets the title for the current menu screen.
    /// </summary>
    /// <returns>A localized string representing the title of the current menu.</returns>
    private static string GetMenuTitle()
    {
        return currentMenu switch
        {
            ConsoleMenu.MainMenu => GetText("mainMenu"),
            ConsoleMenu.Language => GetText("selectLanguage"),
            ConsoleMenu.CreateJob => GetText("createJob"),
            ConsoleMenu.ShowJob => GetText("viewJobs"),
            ConsoleMenu.StartJob => GetText("startJob"),
            ConsoleMenu.StopJob => GetText("stopJob"),
            ConsoleMenu.DeleteJob => GetText("deleteJob"),
            _ => GetText("title")
        };
    }
}
