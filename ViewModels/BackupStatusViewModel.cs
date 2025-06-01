using better_saving.Models;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Reflection;

namespace better_saving.ViewModels
{
    public class BackupStatusViewModel : ViewModelBase, IDisposable
    {
        private readonly backupJob _job;
        private readonly MainViewModel _mainViewModel;
        private readonly string _stateFilePath;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly System.Timers.Timer _refreshTimer;
        private string _status = "En attente";
        private int _progress;
        private long _filesRemaining;
        private long _filesTotal;
        private long _bytesRemaining;
        private long _bytesTotal;

        // Garder uniquement les propriétés pertinentes
        public string JobName => _job.Name;
        public string JobType => _job.Type.ToString();
        public string JobState => _job.State.ToString();
        public byte JobProgress => _job.Progress;
        public long TotalFilesToCopy => _job.TotalFilesToCopy;
        public int NumberFilesLeftToDo => _job.NumberFilesLeftToDo;
        public string? ErrorMessage => _job.ErrorMessage;

        public ICommand StartJobCommand { get; }
        public ICommand PauseJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand ResumeJobCommand { get; }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public long FilesRemaining
        {
            get => _filesRemaining;
            set => SetProperty(ref _filesRemaining, value);
        }

        public long FilesTotal
        {
            get => _filesTotal;
            set => SetProperty(ref _filesTotal, value);
        }

        public BackupStatusViewModel(backupJob job, MainViewModel mainViewModel)
        {
            _job = job;
            _mainViewModel = mainViewModel;

            // Initialiser le chemin du state.json
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _stateFilePath = Path.Combine(logsDirectory, "state.json");

            // Créer un FileSystemWatcher pour surveiller les changements
            _fileWatcher = new FileSystemWatcher(logsDirectory, "state.json");
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            _fileWatcher.Changed += OnStateFileChanged;
            _fileWatcher.EnableRaisingEvents = true;

            // Timer de rafraîchissement
            _refreshTimer = new System.Timers.Timer(1000); // Vérifier chaque seconde
            _refreshTimer.Elapsed += (s, e) => LoadJobStatusFromStateLog();
            _refreshTimer.Start();

            StartJobCommand = new RelayCommand(_ => StartJob(), _ => CanStartJob());
            PauseJobCommand = new RelayCommand(_ => PauseJob(), _ => CanPauseJob());
            StopJobCommand = new RelayCommand(_ => StopJob(), _ => CanStopJob());
            ResumeJobCommand = new RelayCommand(_ => ResumeJob(), _ => CanResumeJob());

            SubscribeToJobEvents();
            
            // Chargement initial
            LoadJobStatusFromStateLog();
        }

        private bool CanStartJob()
        {
            return true;
        }

        private bool CanPauseJob()
        {
            return true;
        }

        private bool CanStopJob()
        {
            return true;
        }

        private bool CanResumeJob()
        {
            return true;
        }

        private async void StartJob()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Tentative de démarrage du job {_job.Name} (État actuel: {_job.State})");
                await _mainViewModel.ListVM.StartJob(_job.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Erreur lors du démarrage du job {_job.Name}: {ex.Message}");
            }
        }

        private async void PauseJob()
        {
            await _mainViewModel.ListVM.PauseJob(_job.Name);
        }

        private async void StopJob()
        {
            await _mainViewModel.ListVM.StopJob(_job.Name);
        }

        private async void ResumeJob()
        {
            await _mainViewModel.ListVM.ResumeJob(_job.Name);
        }

        private void SubscribeToJobEvents()
        {
            if (_job != null)
            {
                _job.PropertyChanged += Job_PropertyChanged;
            }
        }

        public void UnsubscribeFromJobEvents()
        {
            if (_job != null)
            {
                _job.PropertyChanged -= Job_PropertyChanged;
            }
        }

        private void Job_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(backupJob.Name):
                    OnPropertyChanged(nameof(JobName));
                    break;
                case nameof(backupJob.Type):
                    OnPropertyChanged(nameof(JobType));
                    break;
                case nameof(backupJob.State):
                    OnPropertyChanged(nameof(JobState));
                    (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PauseJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
                case nameof(backupJob.Progress):
                    OnPropertyChanged(nameof(JobProgress));
                    break;
                case nameof(backupJob.TotalFilesToCopy):
                    OnPropertyChanged(nameof(TotalFilesToCopy));
                    break;
                case nameof(backupJob.NumberFilesLeftToDo):
                    OnPropertyChanged(nameof(NumberFilesLeftToDo));
                    break;
                case nameof(backupJob.ErrorMessage):
                    OnPropertyChanged(nameof(ErrorMessage));
                    break;
            }
        }

        private void OnStateFileChanged(object sender, FileSystemEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    LoadJobStatusFromStateLog();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du rechargement du state.json pour le status : {ex.Message}");
                }
            });
        }

        private void LoadJobStatusFromStateLog()
        {
            try
            {
                if (File.Exists(_stateFilePath))
                {
                    string jsonContent = File.ReadAllText(_stateFilePath);
                    var jobStates = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

                    if (jobStates != null)
                    {
                        // Trouver le job correspondant dans le state.json
                        var currentJobState = jobStates.FirstOrDefault(j => 
                            j.GetProperty("Name").GetString() == _job.Name);

                        if (currentJobState.ValueKind != JsonValueKind.Undefined)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    // Mettre à jour l'état et la progression
                                    if (currentJobState.TryGetProperty("State", out JsonElement stateElement))
                                    {
                                        string stateStr = stateElement.GetString() ?? JobStates.Idle.ToString();
                                        _job.State = Enum.Parse<JobStates>(stateStr, true);
                                    }

                                    if (currentJobState.TryGetProperty("Progress", out JsonElement progressElement))
                                    {
                                        _job.Progress = (byte)progressElement.GetInt32();
                                    }

                                    if (currentJobState.TryGetProperty("TotalFilesToCopy", out JsonElement totalElement))
                                    {
                                        var jobType = typeof(backupJob);
                                        jobType.GetField("_totalFilesToCopy", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.SetValue(_job, totalElement.GetInt32());
                                    }

                                    if (currentJobState.TryGetProperty("NumberFilesLeftToDo", out JsonElement leftElement))
                                    {
                                        var jobType = typeof(backupJob);
                                        jobType.GetField("_numberFilesLeftToDo", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?.SetValue(_job, leftElement.GetInt32());
                                    }

                                    if (currentJobState.TryGetProperty("ErrorMessage", out JsonElement errorElement))
                                    {
                                        _job.ErrorMessage = errorElement.GetString();
                                    }

                                    // Notifier les changements
                                    OnPropertyChanged(nameof(JobState));
                                    OnPropertyChanged(nameof(JobProgress));
                                    OnPropertyChanged(nameof(TotalFilesToCopy));
                                    OnPropertyChanged(nameof(NumberFilesLeftToDo));
                                    OnPropertyChanged(nameof(ErrorMessage));

                                    // Mettre à jour l'état des commandes
                                    (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                                    (PauseJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                                    (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Erreur lors de la mise à jour du status : {ex.Message}");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement du state.json pour le status : {ex.Message}");
            }
        }

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
                    Console.WriteLine($"Erreur lors de la libération du FileWatcher : {ex.Message}");
                }
            }

            if (_refreshTimer != null)
            {
                try
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la libération du Timer : {ex.Message}");
                }
            }

            UnsubscribeFromJobEvents();
        }
    }
}