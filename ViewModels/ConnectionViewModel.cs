using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;

namespace better_saving.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private TcpClient? _client;
        private bool _isConnected;
        private string _serverAddress = "localhost";
        private int _serverPort = 12345;
        private string _connectionStatus = "Déconnecté";

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                ConnectionStatus = value ? "Connecté" : "Déconnecté";
            }
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        public int ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set => SetProperty(ref _connectionStatus, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public ConnectionViewModel()
        {
            ConnectCommand = new RelayCommand(_ => Connect(), _ => !IsConnected);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        }

        private async Task Connect()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ServerAddress, ServerPort);
                IsConnected = true;
                // TODO: Démarrer la réception des données
                StartReceiving();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Erreur de connexion: {ex.Message}";
                IsConnected = false;
            }
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            IsConnected = false;
        }

        private async void StartReceiving()
        {
            if (_client == null || !_client.Connected) return;

            try
            {
                NetworkStream stream = _client.GetStream();
                byte[] buffer = new byte[1024];

                while (_client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Connexion fermée

                    string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // TODO: Traiter les données reçues et mettre à jour les backups
                    ProcessReceivedData(data);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Erreur de réception: {ex.Message}";
                Disconnect();
            }
        }

        private void ProcessReceivedData(string data)
        {
            // TODO: Implémenter le traitement des données reçues
            // Cette méthode sera appelée chaque fois que des données sont reçues du serveur
        }
    }
} 