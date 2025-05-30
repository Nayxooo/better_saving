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
        private string _serverAddress = "192.168.1.17";
        private string _serverPort = "8989";
        private bool _isConnected;
        private string _connectionStatus = "Non connecté";
        private readonly string _stateFilePath;

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
                ConnectionStatus = "Connecté";
                IsConnected = true;

                // Envoyer un PING initial pour vérifier la connexion
                await SendCommand(RemoteCommands.PING);
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
                    // Tenter de désérialiser le message comme un state.json
                    var jobs = JsonSerializer.Deserialize<List<JsonElement>>(message);
                    if (jobs != null)
                    {
                        // Sauvegarder le state.json reçu
                        File.WriteAllText(_stateFilePath, message);
                        
                        // Mettre à jour les jobs
                        UpdateBackupJobs(jobs);
                    }
                }
                catch (JsonException)
                {
                    // Si ce n'est pas un JSON valide, c'est peut-être une réponse à une commande
                    Console.WriteLine($"Message reçu (non-JSON): {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du traitement du message : {ex.Message}");
                }
            });
        }

        private void UpdateBackupJobs(List<JsonElement> jobStates)
        {
            var logger = _backupListViewModel.GetLogger();
            var updatedJobs = new List<backupJob>();

            foreach (var jobState in jobStates)
            {
                try
                {
                    string name = jobState.GetProperty("Name").GetString() ?? "";
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

            // Mettre à jour la collection de jobs
            _backupListViewModel.Jobs.Clear();
            foreach (var job in updatedJobs)
            {
                _backupListViewModel.Jobs.Add(job);
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
    }
} 