using System;
using System.Windows.Input;
using better_saving.Services;
using System.Threading.Tasks;
using System.Windows;

namespace better_saving.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly SocketService _socketService;
        private string _serverAddress = "169.254.50.74";
        private string _serverPort = "8989";
        private bool _isConnected;
        private string _connectionStatus = "Non connecté";

        public ICommand ConnectCommand { get; }

        public ConnectionViewModel()
        {
            _socketService = new SocketService();
            _socketService.ConnectionStateChanged += OnConnectionStateChanged;
            _socketService.MessageReceived += OnMessageReceived;
            
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
                // Pour l'instant on affiche juste dans la console
                Console.WriteLine($"Message reçu: {message}");
                
                // TODO: Parser le JSON et mettre à jour la liste des backups
                // Si le message est un state.json, il faudra le désérialiser et mettre à jour l'interface
            });
        }
    }
} 