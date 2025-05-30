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

namespace better_saving.ViewModels
{
    public class BackupListViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private ObservableCollection<backupJob> _jobs = new ObservableCollection<backupJob>();
        private readonly Logger _logger;
        private backupJob? _selectedBackupJob;
        private readonly string _stateFilePath;

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

            // Définir le chemin du fichier state.json dans le dossier logs
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _stateFilePath = Path.Combine(logsDirectory, "state.json");

            LoadJobsFromStateLog();
        }

        public Logger GetLogger()
        {
            return _logger;
        }

        private void LoadJobsFromStateLog()
        {
            try
            {
                if (File.Exists(_stateFilePath))
                {
                    string jsonContent = File.ReadAllText(_stateFilePath);
                    var jobStates = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

                    if (jobStates != null)
                    {
                        var loadedJobs = new List<backupJob>();

                        foreach (var jobState in jobStates)
                        {
                            try
                            {
                                string name = jobState.GetProperty("Name").GetString() ?? "";
                                string sourceDir = jobState.GetProperty("SourceDirectory").GetString() ?? "";
                                string targetDir = jobState.GetProperty("TargetDirectory").GetString() ?? "";
                                string typeStr = jobState.GetProperty("Type").GetString() ?? JobType.Full.ToString();
                                JobType type = Enum.Parse<JobType>(typeStr, true);

                                var job = new backupJob(name, sourceDir, targetDir, type, _logger);

                                if (jobState.TryGetProperty("State", out JsonElement stateElement))
                                {
                                    string stateStr = stateElement.GetString() ?? JobStates.Idle.ToString();
                                    job.State = Enum.Parse<JobStates>(stateStr, true);
                                }

                                if (jobState.TryGetProperty("Progress", out JsonElement progressElement))
                                {
                                    job.Progress = (byte)progressElement.GetInt32();
                                }

                                if (jobState.TryGetProperty("ErrorMessage", out JsonElement errorElement))
                                {
                                    job.ErrorMessage = errorElement.GetString();
                                }

                                loadedJobs.Add(job);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erreur lors du chargement d'un job : {ex.Message}");
                            }
                        }

                        Jobs = new ObservableCollection<backupJob>(loadedJobs);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement du state.json : {ex.Message}");
            }
        }

        public void AddJob(backupJob job)
        {
            Jobs.Add(job);
            _logger.UpdateAllJobsState();
            OnPropertyChanged(nameof(Jobs));
        }

        public void RemoveJob(backupJob jobToRemove)
        {
            if (Jobs.Contains(jobToRemove))
            {
                Jobs.Remove(jobToRemove);
                if (Jobs.Count > 0)
                {
                    _logger.UpdateAllJobsState();
                }
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
                _logger.UpdateAllJobsState();
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
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.START_JOB);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du démarrage du job : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task PauseJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.PAUSE_JOB);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la mise en pause du job : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task StopJob(string jobName)
        {
            try
            {
                await _mainViewModel.ConnectionVM.SendCommand(RemoteCommands.STOP_JOB);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'arrêt du job : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}