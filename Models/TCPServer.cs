using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using better_saving.ViewModels; // Added for MainViewModel
using better_saving.Models;
using System.Text.Json.Nodes;
using System.Text.Json; // Added for JobType, JobStates, Logger

namespace better_saving.Models
{
    public class TCPServer
    {
        private TcpListener? _listener;
        private readonly List<BinaryWriter> _clientWriters = [];
        private readonly object _clientsLock = new();
        private string? StateLogFilePath;
        private string? stateJsonContent;
        private string? tempStateJsonContent;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenTask;
        private readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/TcpServer.log");


        private const int DefaultPort = 8989;
        public bool IsRunning { get; private set; }
        public int Port { get; private set; }
        private readonly MainViewModel _mainViewModel; // Added MainViewModel reference

        // Event for logging messages to WPF UI
        public event Action<string>? LogMessage;

        public TCPServer(MainViewModel mainViewModel) // Updated constructor
        {
            Port = DefaultPort;
            IsRunning = false;
            _mainViewModel = mainViewModel; // Store MainViewModel instance

            // reset the log file at startup
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath); // Clear log file at startup
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to clear log file at startup: {ex.Message}");
            }
        }

        public void SetStateFilePath(string stateFilePath)
        {
            StateLogFilePath = stateFilePath;
        }

        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            string logMessage = $"[{timestamp}] [TCPServer] {message}";

            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fallback to debug output if file writing fails
                Debug.WriteLine($"[TCPServer] Failed to write to log file: {ex.Message}");
                Debug.WriteLine(logMessage); // Log original message to debug if file write fails
            }

            try
            {
                LogMessage?.Invoke(message); // Invoke event subscribers
            }
            catch (Exception ex)
            {
                // Log exceptions from subscribers to prevent server crash
                Debug.WriteLine($"[TCPServer] Exception in LogMessage event subscriber: {ex.Message}");
                Debug.WriteLine($"[TCPServer] Original log message during subscriber exception: {message}");
            }
            Debug.WriteLine(logMessage); // Log to debug output regardless
        }

        public string GetServerAddress()
        {
            if (!IsRunning) return "";
            string localIP = GetLocalIPAddress();
            return $"{localIP}:{Port}";
        }

        private string GetLocalIPAddress()
        {
            try
            {
                // Get all network interfaces that are up and operational
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var networkInterface in networkInterfaces)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    var unicastAddresses = ipProperties.UnicastAddresses;

                    foreach (var unicastAddress in unicastAddresses)
                    {
                        var ip = unicastAddress.Address;

                        // Only consider IPv4 addresses that are not loopback or link-local
                        if (ip.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip) &&
                            !ip.ToString().StartsWith("169.254")) // Exclude link-local addresses
                        {
                            return ip.ToString();
                        }
                    }
                }

                // Fallback to the original method if no suitable interface found
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var fallbackIp = host.AddressList.FirstOrDefault(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip));

                return fallbackIp?.ToString() ?? "127.0.0.1";
            }
            catch (Exception)
            {
                return "127.0.0.1";
            }
        }

        public void Start(int port = DefaultPort)
        {
            if (IsRunning)
            {
                Log($"TCP Server is already running on port {Port}");
                return;
            }

            Port = port;
            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);

            try
            {
                _listener.Start();
                IsRunning = true;
                _listenTask = ListenForClientsAsync(_cancellationTokenSource.Token);
                Log($"TCP Server successfully started on {GetLocalIPAddress()}:{Port}");
                Log($"Server is listening for connections on all interfaces (0.0.0.0:{Port})");
            }
            catch (SocketException ex)
            {
                Log($"Failed to start TCP server on port {Port}: {ex.Message}");
                Log($"Error code: {ex.ErrorCode}");
                if (ex.ErrorCode == 10048) // Port already in use
                {
                    Log("Port is already in use. Try a different port or stop the application using this port.");
                }
                IsRunning = false;
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                Log($"Unexpected error starting TCP server: {ex.Message}");
                IsRunning = false;
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task ListenForClientsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_listener == null)
                    {
                        Log("TCP listener is null, stopping listen loop.");
                        break;
                    }

                    TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    Log("Client connected.");
                    // Handle client in a new task to avoid blocking the listener loop
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
            }
            catch (OperationCanceledException)
            {
                Log("TCP server listening cancelled.");
            }
            catch (SocketException ex) when (token.IsCancellationRequested)
            {
                Log($"SocketException during shutdown: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"Error in listening loop: {ex.Message}");
                // Consider if the server should attempt to restart or log this severely
            }
            finally
            {
                IsRunning = false;
                Log("TCP Server stopped listening.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            BinaryWriter? clientBinaryWriter = null; // To ensure removal in finally block
            try
            {
                using (client) // Ensure client is disposed
                using (var stream = client.GetStream())
                // Using BinaryReader and BinaryWriter for length-prefixed messages
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    clientBinaryWriter = writer; // Assign for use in finally block
                    lock (_clientsLock)
                    {
                        _clientWriters.Add(clientBinaryWriter);
                    }
                    Log($"Client {client.Client.RemoteEndPoint} connected and writer added.");

                    // Example: Send welcome message with length prefix
                    string welcomeMessage = "Welcome to Better Saving TCP Server!";
                    byte[] welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                    writer.Write(welcomeBytes.Length); // Send length
                    writer.Write(welcomeBytes); // Send message
                    await writer.BaseStream.FlushAsync(cts.Token);


                    while (!cts.Token.IsCancellationRequested && client.Connected)
                    {
                        // Read length prefix
                        int messageLength;
                        try
                        {
                            messageLength = reader.ReadInt32(); // Reads 4-byte integer for length
                        }
                        catch (EndOfStreamException)
                        {
                            // Client disconnected gracefully while expecting length
                            Log($"Client {client.Client.RemoteEndPoint} disconnected gracefully while expecting message length.");
                            break;
                        }
                        catch (IOException ex)
                        {
                            // Client disconnected
                            Log($"IO Exception while reading message length from {client.Client.RemoteEndPoint}: {ex.Message}. Assuming disconnection.");
                            break;
                        }

                        if (messageLength == 0)
                        {
                            Log($"Received zero-length message from {client.Client.RemoteEndPoint}, treating as keep-alive or no-op.");
                            continue;
                        }

                        // Read message content
                        byte[] messageBytes = reader.ReadBytes(messageLength);
                        string receivedMessage = Encoding.UTF8.GetString(messageBytes);

                        Log($"Received from {client.Client.RemoteEndPoint}: {receivedMessage}");

                        // Parse command and arguments
                        string[] parts = receivedMessage.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        string command = parts[0].ToUpperInvariant();
                        RemoteCommands remoteCommand = Enum.TryParse<RemoteCommands>(command, out var parsedCommand) ? parsedCommand : RemoteCommands.UNKNOWN;

                        string argsString = parts.Length > 1 ? parts[1] : string.Empty;
                        string responseMessage;
                        string[] commandArgs = argsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); // Assumes no spaces in individual args



                        switch (remoteCommand)
                        {
                            case RemoteCommands.PING:
                                responseMessage = "PONG";
                                Log($"Responding PONG to {client.Client.RemoteEndPoint}");
                                break;
                            case RemoteCommands.START_JOB:
                                if (commandArgs.Length >= 1)
                                {
                                    string jobNameToStart = commandArgs[0];
                                    var jobToStart = _mainViewModel.ListVM.Jobs.FirstOrDefault(j => j.Name.Equals(jobNameToStart, StringComparison.OrdinalIgnoreCase));
                                    if (jobToStart != null)
                                    {
                                        if (jobToStart.State == JobStates.Working)
                                        {
                                            responseMessage = $"ERROR: Job '{jobToStart.Name}' is already running.";
                                        }
                                        else if (_mainViewModel.IsSoftwareRunning())
                                        {
                                            // send back error message that a software is blocking the job start
                                            responseMessage = $"ERROR: Cannot start job '{jobToStart.Name}' because a blocked software is running.";
                                            Log($"Cannot start job '{jobToStart.Name}' because a blocked software is running. Command received from {client.Client.RemoteEndPoint}.");
                                        }
                                        else
                                        {
                                            _ = Task.Run(jobToStart.Start, token);
                                            responseMessage = $"SUCCESS: Job '{jobToStart.Name}' starting.";
                                            Log($"Attempting to start job '{jobToStart.Name}' via TCP command from {client.Client.RemoteEndPoint}.");
                                        }
                                    }
                                    else
                                    {
                                        Log($"Job lookup failed for command {command} with name: '[{jobNameToStart}]'. Available job names: {string.Join(", ", _mainViewModel.ListVM.Jobs.Select(j => $"['{j.Name}]"))}");
                                        responseMessage = $"ERROR: Job '{jobNameToStart}' not found.";
                                    }
                                }
                                else
                                {
                                    responseMessage = "ERROR: START_JOB requires 1 argument: jobName";
                                }
                                break;
                            case RemoteCommands.RESUME_JOB:
                                if (commandArgs.Length >= 1)
                                {
                                    string jobNameToResume = commandArgs[0];
                                    var jobToResume = _mainViewModel.ListVM.Jobs.FirstOrDefault(j => j.Name.Equals(jobNameToResume, StringComparison.OrdinalIgnoreCase));
                                    if (jobToResume != null)
                                    {
                                        if (jobToResume.State == JobStates.Paused)
                                        {
                                            _ = Task.Run(jobToResume.Resume, token);
                                            responseMessage = $"SUCCESS: Job '{jobToResume.Name}' resumed.";
                                            Log($"Job '{jobToResume.Name}' resumed via TCP command from {client.Client.RemoteEndPoint}.");
                                        }
                                        else
                                        {
                                            responseMessage = $"ERROR: Job '{jobToResume.Name}' cannot be resumed (state: {jobToResume.State}).";
                                        }
                                    }
                                    else
                                    {
                                        Log($"Job lookup failed for command {command} with name: '[{jobNameToResume}]'. Available job names: {string.Join(", ", _mainViewModel.ListVM.Jobs.Select(j => $"['{j.Name}']"))}");
                                        responseMessage = $"ERROR: Job '{jobNameToResume}' not found.";
                                    }
                                }
                                else
                                {
                                    responseMessage = "ERROR: RESUME_JOB requires 1 argument: jobName";
                                }
                                break;
                            case RemoteCommands.PAUSE_JOB:
                                if (commandArgs.Length >= 1)
                                {
                                    string jobNameToPause = commandArgs[0];
                                    var jobToPause = _mainViewModel.ListVM.Jobs.FirstOrDefault(j => j.Name.Equals(jobNameToPause, StringComparison.OrdinalIgnoreCase));
                                    if (jobToPause != null)
                                    {
                                        if (jobToPause.State == JobStates.Working)
                                        {
                                            // simulare a button press 
                                            jobToPause.Pause();
                                            responseMessage = $"SUCCESS: Job '{jobToPause.Name}' paused.";
                                            Log($"Job '{jobToPause.Name}' paused via TCP command from {client.Client.RemoteEndPoint}.");
                                        }
                                        else
                                        {
                                            responseMessage = $"ERROR: Job '{jobToPause.Name}' cannot be paused (state: {jobToPause.State}).";
                                        }
                                    }
                                    else
                                    {
                                        Log($"Job lookup failed for command {command} with name: '[{jobNameToPause}]'. Available job names: {string.Join(", ", _mainViewModel.ListVM.Jobs.Select(j => $"['{j.Name}']"))}");
                                        responseMessage = $"ERROR: Job '{jobNameToPause}' not found.";
                                    }
                                }
                                else
                                {
                                    responseMessage = "ERROR: PAUSE_JOB requires 1 argument: jobName";
                                }
                                break;
                            case RemoteCommands.STOP_JOB:
                                if (commandArgs.Length >= 1)
                                {
                                    string jobNameToStop = commandArgs[0];
                                    var jobToStop = _mainViewModel.ListVM.Jobs.FirstOrDefault(j => j.Name.Equals(jobNameToStop, StringComparison.OrdinalIgnoreCase));
                                    if (jobToStop != null)
                                    {
                                        if (jobToStop.State == JobStates.Working || jobToStop.State == JobStates.Paused)
                                        {
                                            jobToStop.Stop();
                                            responseMessage = $"SUCCESS: Job '{jobToStop.Name}' stopped.";
                                            Log($"Job '{jobToStop.Name}' stopped via TCP command from {client.Client.RemoteEndPoint}.");
                                        }
                                        else
                                        {
                                            responseMessage = $"ERROR: Job '{jobToStop.Name}' cannot be stopped (state: {jobToStop.State}).";
                                        }
                                    }
                                    else
                                    {
                                        Log($"Job lookup failed for command {command} with name: '[{jobNameToStop}]'. Available job names: {string.Join(", ", _mainViewModel.ListVM.Jobs.Select(j => $"['{j.Name}']"))}");
                                        responseMessage = $"ERROR: Job '{jobNameToStop}' not found.";
                                    }
                                }
                                else
                                {
                                    responseMessage = "ERROR: STOP_JOB requires 1 argument: jobName";
                                }
                                break;
                            case RemoteCommands.GET_JOBS:
                                string? jobsState = GetJobsStateContent();
                                if (!string.IsNullOrEmpty(jobsState))
                                {
                                    responseMessage = jobsState;
                                    Log($"Sending job states to {client.Client.RemoteEndPoint}");
                                }
                                else
                                {
                                    responseMessage = "ERROR: Could not retrieve job states.";
                                    Log($"Failed to retrieve job states for {client.Client.RemoteEndPoint}. StateLogFilePath: {StateLogFilePath}");
                                }
                                break;
                            default:
                                responseMessage = "ERROR: Unknown command.";
                                Log($"Received unknown command '{command}' from {client.Client.RemoteEndPoint}.");
                                break;
                        }

                        // Send response
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        writer.Write(responseBytes.Length); // Send length
                        writer.Write(responseBytes); // Send response
                        await writer.BaseStream.FlushAsync(cts.Token); // Ensure data is sent

                        Log($"Sent response to {client.Client.RemoteEndPoint}: {responseMessage}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log($"Client connection {client.Client.RemoteEndPoint} cancelled gracefully.");
            }
            catch (Exception ex)
            {
                Log($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
            }
            finally
            {
                if (clientBinaryWriter != null)
                {
                    lock (_clientsLock)
                    {
                        _clientWriters.Remove(clientBinaryWriter);
                    }
                    Log($"Client {client.Client.RemoteEndPoint} disconnected and writer removed.");
                }
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                Log("TCP Server stopped.");
            }
            catch (Exception ex)
            {
                Log($"Error stopping TCP server: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Gets the content of the state.json file, removing sensitive information.
        /// </summary>
        /// <returns></returns>
        private string? GetJobsStateContent()
        {
            if (string.IsNullOrWhiteSpace(StateLogFilePath))
            {
                Log("StateLogFilePath is not set, cannot get state JSON content.");
                return null;
            }

            try
            {
                string content = File.ReadAllText(StateLogFilePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log($"State file {StateLogFilePath} is empty or whitespace. Interpreting as empty job list [].");
                    return "[]"; // Treat empty/whitespace file as an empty job list
                }

                JsonNode? parsedNode = JsonNode.Parse(content);
                JsonArray? stateJsonArray = parsedNode?.AsArray();

                if (stateJsonArray == null)
                {
                    Log($"Error parsing state file {StateLogFilePath} as JSON array (or content was not an array). Content: '{content}'. Interpreting as empty job list [].");
                    return "[]"; // Treat non-array JSON or parse error as an empty job list
                }

                // Remove sensitive information like directory paths
                for (int i = stateJsonArray.Count - 1; i >= 0; i--)
                {
                    if (stateJsonArray[i] is JsonObject jobObject)
                    {
                        jobObject.Remove("SourceDirectory");
                        jobObject.Remove("TargetDirectory");
                    }
                }

                // Convert the modified state JSON back to a string
                return stateJsonArray.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            }
            catch (FileNotFoundException)
            {
                Log($"Error: State file not found at {StateLogFilePath}");
                return null;
            }
            catch (JsonException jsonEx)
            {
                Log($"Error parsing JSON from state file {StateLogFilePath}: {jsonEx.Message}. Interpreting as empty job list [].");
                return "[]"; // Treat JSON parsing errors as an empty job list
            }
            catch (Exception ex)
            {
                Log($"Error reading or processing state file {StateLogFilePath}: {ex.Message}");
                return null; // For other unexpected errors, return null
            }
        }
        

        public void HandleStateFileUpdate()
        {
            try
            {
                if (!IsRunning)
                {
                    Log("Server not running, skipping state file update.");
                    return;
                }

                string? newProcessedState = GetJobsStateContent();
                if (newProcessedState == null)
                {
                    Log("Failed to retrieve state.json content; broadcast aborted.");
                    return;
                }

                // Ne pas retourner si le contenu est le m�me, car il peut y avoir des mises � jour partielles
                this.stateJsonContent = newProcessedState;
                this.tempStateJsonContent = newProcessedState;

                byte[] stateBytes = Encoding.UTF8.GetBytes(this.stateJsonContent);

                List<BinaryWriter> clientsToRemove = [];

                lock (_clientsLock)
                {
                    if (_clientWriters.Count == 0)
                    {
                        Log("No clients connected, state.json updated locally only.");
                        return;
                    }

                    foreach (var writer in _clientWriters.ToList()) // Utiliser ToList() pour �viter les modifications concurrentes
                    {
                        try
                        {
                            writer.Write(stateBytes.Length);
                            writer.Write(stateBytes);
                            writer.Flush();
                        }
                        catch (Exception ex)
                        {
                            Log($"Error sending state update to client: {ex.Message}");
                            clientsToRemove.Add(writer);
                        }
                    }

                    // Nettoyer les clients probl�matiques
                    foreach (var client in clientsToRemove)
                    {
                        _clientWriters.Remove(client);
                        try { client.Close(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error in HandleStateFileUpdate: {ex.Message}");
            }
        }
    }
}
