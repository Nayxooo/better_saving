using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace better_saving.Services
{
    public class SocketService
    {
        private Socket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;
        private const int CONNECTION_TIMEOUT_MS = 20000; // 20 secondes
        
        public event EventHandler<string> MessageReceived;
        public event EventHandler<bool> ConnectionStateChanged;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStateChanged?.Invoke(this, value);
                }
            }
        }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Créer une tâche de connexion avec timeout
                using (var timeoutCts = new CancellationTokenSource(CONNECTION_TIMEOUT_MS))
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            var connectResult = _socket.BeginConnect(endPoint, null, null);
                            bool success = connectResult.AsyncWaitHandle.WaitOne(CONNECTION_TIMEOUT_MS, true);
                            
                            if (!success)
                            {
                                _socket.Close();
                                throw new TimeoutException("La connexion n'a pas pu être établie dans le délai imparti (20 secondes).");
                            }
                            
                            _socket.EndConnect(connectResult);
                        }, timeoutCts.Token);

                        IsConnected = true;
                        // Démarrer l'écoute des messages dans un thread séparé
                        _ = ListenForMessagesAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TimeoutException("La connexion n'a pas pu être établie dans le délai imparti (20 secondes).");
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                throw new Exception($"Erreur de connexion: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_socket != null && _socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                
                IsConnected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la déconnexion: {ex.Message}");
            }
            finally
            {
                _socket = null;
                _cancellationTokenSource = null;
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _socket != null && _socket.Connected)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = await Task.Run(() => _socket.Receive(buffer), cancellationToken);
                    
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Annulation normale, pas besoin de traitement particulier
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écoute des messages: {ex.Message}");
                IsConnected = false;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected || _socket == null)
            {
                throw new InvalidOperationException("Non connecté au serveur");
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await Task.Run(() => _socket.Send(buffer));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi du message: {ex.Message}");
                throw;
            }
        }
    }
} 