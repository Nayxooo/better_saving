using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using better_saving.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Reflection;

namespace better_saving.ViewModels
{
    public class BackupListViewModel : ViewModelBase, IDisposable
    {
        private readonly MainViewModel _mainViewModel;
        private ObservableCollection<backupJob> _jobs = new ObservableCollection<backupJob>();
        private readonly Logger _logger;
        private backupJob? _selectedBackupJob;
        private readonly string _stateFilePath;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly System.Timers.Timer _refreshTimer; // Réajout du timer

        public ObservableCollection<backupJob> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        public backupJob? SelectedBackupJob
        {
            get => _selectedBackupJob;
            set
            {
                if (SetProperty(ref _selectedBackupJob, value) && value != null)
                {
                    _mainViewModel.ShowJobStatus(value);
                }
            }
        }

        public BackupListViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _logger = new Logger();
            _logger.SetJobProvider(() => Jobs);

            // Chemin du fichier state.json
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _stateFilePath = Path.Combine(logsDirectory, "state.json");

            // Créer un FileSystemWatcher pour surveiller les changements
            _fileWatcher = new FileSystemWatcher(logsDirectory, "state.json");
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            _fileWatcher.Changed += OnStateFileChanged;
            _fileWatcher.EnableRaisingEvents = true;

            // Remettre le timer de rafraîchissement
            _refreshTimer = new System.Timers.Timer(1000); // Vérifier chaque seconde
            _refreshTimer.Elapsed += (s, e) => LoadJobsFromStateLog();
            _refreshTimer.Start();

            // Chargement initial
            LoadJobsFromStateLog();
        }

        private void OnStateFileChanged(object sender, FileSystemEventArgs e)
        {
            // Utiliser le Dispatcher pour mettre à jour l'UI thread-safe
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    LoadJobsFromStateLog();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while reloading state.json: {ex.Message}");
                }
            });
        }

        private void LoadJobsFromStateLog()
        {
            try
            {
                if (File.Exists(_stateFilePath))
                {
                    string jsonContent = File.ReadAllText(_stateFilePath);
                    // Log pour debug uniquement
                    // Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Contenu du state.json lu : {jsonContent}");

                    var jobStates = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

                    if (jobStates != null)
                    {
                        var loadedJobs = new List<backupJob>();

                        foreach (var jobState in jobStates)
                        {
                            try
                            {
                                string name = jobState.GetProperty("Name").GetString() ?? "";
                                string typeStr = jobState.GetProperty("Type").GetString() ?? "Full";
                                string stateStr = jobState.GetProperty("State").GetString() ?? "Idle";
                                int totalFilesToCopy = jobState.GetProperty("TotalFilesToCopy").GetInt32();
                                ulong totalFilesSize = (ulong)jobState.GetProperty("TotalFilesSize").GetInt64();
                                int numberFilesLeftToDo = jobState.GetProperty("NumberFilesLeftToDo").GetInt32();
                                double progress = jobState.GetProperty("Progress").GetDouble();
                                string? errorMessage = jobState.TryGetProperty("ErrorMessage", out JsonElement errorElement) 
                                    ? errorElement.GetString() 
                                    : null;

                                // Créer le job avec les valeurs par défaut pour source/target directory
                                var job = new backupJob(name, "", "", Enum.Parse<JobType>(typeStr), _logger);

                                // Mettre à jour les propriétés avec reflection
                                var jobType = typeof(backupJob);
                                jobType.GetField("_totalFilesToCopy", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.SetValue(job, totalFilesToCopy);
                                
                                jobType.GetField("_totalSizeToCopy", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.SetValue(job, totalFilesSize);
                                
                                jobType.GetField("_numberFilesLeftToDo", BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?.SetValue(job, numberFilesLeftToDo);

                                // Définir l'état et la progression
                                job.State = Enum.Parse<JobStates>(stateStr);
                                job.Progress = (byte)Math.Min(100, Math.Max(0, Math.Round(progress)));
                                
                                // Définir le message d'erreur si présent
                                if (errorMessage != null)
                                {
                                    job.ErrorMessage = errorMessage;
                                }

                                loadedJobs.Add(job);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while loading job: {ex.Message}");
                            }
                        }

                        Jobs = new ObservableCollection<backupJob>(loadedJobs);
                        OnPropertyChanged(nameof(Jobs));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading state.json: {ex.Message}");
            }
        }

        public void AddJob(backupJob job)
        {
            Jobs.Add(job);
            OnPropertyChanged(nameof(Jobs));
        }

        public void RemoveJob(backupJob jobToRemove)
        {
            if (Jobs.Contains(jobToRemove))
            {
                Jobs.Remove(jobToRemove);
                OnPropertyChanged(nameof(Jobs));
            }
        }

        public void UpdateBackupJob(backupJob updatedJob)
        {
            var existingJob = Jobs.FirstOrDefault(j => j.Name == updatedJob.Name);
            if (existingJob != null)
            {
                var index = Jobs.IndexOf(existingJob);
                Jobs[index] = updatedJob;
                OnPropertyChanged(nameof(Jobs));
            }
        }

        public void RemoveBackupJob(string jobName)
        {
            var jobToRemove = Jobs.FirstOrDefault(j => j.Name == jobName);
            if (jobToRemove != null)
            {
                RemoveJob(jobToRemove);
            }
        }

        public async Task StartJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.START_JOB, jobName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error starting job: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task PauseJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.PAUSE_JOB, jobName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error pausing job: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task StopJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.STOP_JOB, jobName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error stopping job: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task ResumeJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.RESUME_JOB, jobName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error resuming job: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void NotifyJobsChanged() => OnPropertyChanged(nameof(Jobs));

        public Logger GetLogger()
        {
            return _logger;
        }

        // N'oubliez pas de libérer les ressources
        public void Dispose()
        {
            if (_fileWatcher != null)
            {
                try
                {
                    _fileWatcher.EnableRaisingEvents = false;
                    _fileWatcher.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error releasing FileWatcher: {ex.Message}");
                }
            }

            // Ajouter le nettoyage du timer
            if (_refreshTimer != null)
            {
                try
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error releasing Timer: {ex.Message}");
                }
            }
        }
    }
}
