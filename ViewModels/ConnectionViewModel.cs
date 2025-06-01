using System;
using System.Windows.Input;
using better_saving.Services;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using better_saving.Models;
using System.Collections.Generic;
using System.IO;

namespace better_saving.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly SocketService _socketService;
        private readonly BackupListViewModel _backupListViewModel;
        private string _serverAddress = "192.168.1.26";
        private string _serverPort = "8989";
        private bool _isConnected;
        private string _connectionStatus = "Non connecté";
        private readonly string _stateFilePath;
        private readonly System.Timers.Timer _refreshTimer;

        public ICommand ConnectCommand { get; }

        public ConnectionViewModel(BackupListViewModel backupListViewModel)
        {
            _socketService = new SocketService();
            _backupListViewModel = backupListViewModel;
            _socketService.ConnectionStateChanged += OnConnectionStateChanged;
            _socketService.MessageReceived += OnMessageReceived;
            
            // Définir le chemin du fichier state.json dans le dossier logs
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            _stateFilePath = Path.Combine(logsDirectory, "state.json");
            
            // Remettre le timer de rafraîchissement
            _refreshTimer = new System.Timers.Timer(1000); // Vérifier chaque seconde
            _refreshTimer.Elapsed += (s, e) => 
            {
                // S'assurer que l'appel se fait sur le thread UI
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    if (IsConnected)
                    {
                        SendCommand(RemoteCommands.GET_JOBS).ConfigureAwait(false);
                    }
                });
            };
            _refreshTimer.Start();
            
            ConnectCommand = new RelayCommand(_ => ExecuteConnectCommand());
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                _serverAddress = value;
                OnPropertyChanged();
            }
        }

        public string ServerPort
        {
            get => _serverPort;
            set
            {
                _serverPort = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        private async void ExecuteConnectCommand()
        {
            if (IsConnected)
            {
                Disconnect();
            }
            else
            {
                await Connect();
            }
        }

        private async Task Connect()
        {
            try
            {
                if (!int.TryParse(_serverPort, out int port))
                {
                    System.Windows.MessageBox.Show("Le port doit être un nombre valide", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ConnectionStatus = "Connexion en cours...";
                await _socketService.ConnectAsync(_serverAddress, port);
                
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Connecté au serveur {_serverAddress}:{port}");
                ConnectionStatus = "Connecté";
                IsConnected = true;
                
                // Un seul GET_JOBS à la connexion pour initialiser
                await SendCommand(RemoteCommands.GET_JOBS);
            }
            catch (TimeoutException)
            {
                ConnectionStatus = "Timeout de connexion";
                System.Windows.MessageBox.Show(
                    "Impossible de se connecter au serveur après 20 secondes d'attente.\nVérifiez que le serveur est bien démarré et accessible.",
                    "Erreur de connexion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                IsConnected = false;
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Erreur de connexion";
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la connexion :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                IsConnected = false;
            }
        }

        private void Disconnect()
        {
            try
            {
                _socketService.Disconnect();
                ConnectionStatus = "Déconnecté";
                IsConnected = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la déconnexion : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnConnectionStateChanged(object sender, bool isConnected)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
                ConnectionStatus = isConnected ? "Connecté" : "Déconnecté";
            });
        }

        private void OnMessageReceived(object sender, string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Message reçu du serveur");
                    
                    // Si le message commence par [, c'est un state.json
                    if (message.Trim().StartsWith("["))
                    {
                        try
                        {
                            // Sauvegarder dans logs/state.json
                            File.WriteAllText(_stateFilePath, message);
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] State.json mis à jour");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Erreur lors de la mise à jour du state.json : {ex.Message}");
                        }
                    }
                    else
                    {
                        // Message normal du serveur (non-JSON)
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Message reçu : {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Erreur lors du traitement du message : {ex.Message}");
                }
            });
        }

        private void UpdateBackupJobs(List<JsonElement> jobStates)
        {
            try
            {
                var logger = _backupListViewModel.GetLogger();
                var updatedJobs = new List<backupJob>();

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Début mise à jour des jobs. Nombre de jobs reçus: {jobStates.Count}");

                foreach (var jobState in jobStates)
                {
                    try
                    {
                        string name = jobState.GetProperty("Name").GetString() ?? "";
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Traitement du job: {name}");

                        string sourceDir = jobState.GetProperty("SourceDirectory").GetString() ?? "";
                        string targetDir = jobState.GetProperty("TargetDirectory").GetString() ?? "";
                        string typeStr = jobState.GetProperty("Type").GetString() ?? JobType.Full.ToString();
                        JobType type = Enum.Parse<JobType>(typeStr, true);

                        var job = new backupJob(name, sourceDir, targetDir, type, logger);

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

                        updatedJobs.Add(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de la mise à jour d'un job : {ex.Message}");
                    }
                }

                // Mettre à jour la collection de jobs sur le thread UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Mise à jour de l'UI avec {updatedJobs.Count} jobs");
                        _backupListViewModel.Jobs.Clear();
                        
                        foreach (var job in updatedJobs)
                        {
                            _backupListViewModel.Jobs.Add(job);
                        }

                        // Forcer la mise à jour de l'UI
                        _backupListViewModel.NotifyJobsChanged();
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] UI mise à jour avec succès");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Erreur lors de la mise à jour de l'UI: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}] Erreur générale dans UpdateBackupJobs: {ex.Message}");
            }
        }

        public async Task SendCommand(RemoteCommands command)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Non connecté au serveur");
            }

            try
            {
                string commandStr = command.ToString();
                await _socketService.SendMessageAsync(commandStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi de la commande : {ex.Message}");
                throw;
            }
        }

        public async Task SendCommand(RemoteCommands command, string? jobName = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Non connecté au serveur");
            }

            try
            {
                string commandStr = command.ToString();
                if (jobName != null)
                {
                    commandStr += $" {jobName}";
                }
                await _socketService.SendMessageAsync(commandStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi de la commande : {ex.Message}");
                throw;
            }
        }
    }
} 