using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// Manages TCP client connections for the game server.
    /// </summary>
    public class NetworkServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly object _clientsLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public NetworkServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the server and begins listening for client connections.
        /// </summary>
        public void Start()
        {
            try
            {
                _listener.Start();
                Console.WriteLine($"Server started on port {_listener.LocalEndpoint}");
                Task.Run(() => ListenForClients(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        private async Task ListenForClients(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    HandleNewClient(client);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the server is stopped
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }

        private void HandleNewClient(TcpClient client)
        {
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            Task.Run(() => ProcessClient(client));
        }

        private async Task ProcessClient(TcpClient client)
        {
            try
            {
                using var reader = new StreamReader(client.GetStream(), leaveOpen: true);
                var connectionType = await reader.ReadLineAsync();

                switch (connectionType)
                {
                    case "PRIMARY":
                        Console.WriteLine($"Connection from {client.Client.RemoteEndPoint} established as PRIMARY.");
                        lock (_clientsLock)
                        {
                            _clients.Add(client);
                        }
                        await HandleClientCommunication(client);
                        break;

                    case "ASSET":
                        await HandleAssetRequestConnection(client, reader);
                        client.Close();
                        break;

                    default:
                        Console.WriteLine($"Unknown connection type '{connectionType}' from {client.Client.RemoteEndPoint}. Closing.");
                        client.Close();
                        break;
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine($"Client disconnected abruptly: {client.Client.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing client {client.Client.RemoteEndPoint}: {ex.Message}");
                if (client.Connected) client.Close();
            }
        }

        private async Task HandleClientCommunication(TcpClient client)
        {
            await SendAssetList(client);

            try
            {
                using (var reader = new StreamReader(client.GetStream(), leaveOpen: true))
                {
                    while (client.Connected)
                    {
                        var message = await reader.ReadLineAsync();
                        if (message == null) break;
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                // Client disconnected
            }
            finally
            {
                Console.WriteLine($"Primary client disconnected: {client.Client.RemoteEndPoint}");
                lock(_clientsLock)
                {
                    _clients.Remove(client);
                }
                client.Close();
            }
        }

        private async Task SendAssetList(TcpClient client)
        {
            try
            {
                var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
                if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

                var assetFiles = Directory.GetFiles(assetsDir);
                var assetNames = assetFiles.Select(Path.GetFileName).ToList();

                var message = string.Join(",", assetNames);
                using (var writer = new StreamWriter(client.GetStream(), leaveOpen: true) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(message);
                }

                Console.WriteLine($"Sent asset list to client: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending asset list: {ex.Message}");
            }
        }

        private async Task HandleAssetRequestConnection(TcpClient client, StreamReader reader)
        {
            try
            {
                using (var writer = new StreamWriter(client.GetStream(), leaveOpen: true) { AutoFlush = true })
                {
                    var assetName = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(assetName)) return;

                    var assetPath = Path.Combine(AppContext.BaseDirectory, "assets", assetName);
                    if (File.Exists(assetPath))
                    {
                        var fileBytes = await File.ReadAllBytesAsync(assetPath);
                        var base64Content = Convert.ToBase64String(fileBytes);
                        await writer.WriteLineAsync(base64Content);
                        Console.WriteLine($"Sent asset '{assetName}' to client via ASSET connection.");
                    }
                    else
                    {
                        await writer.WriteLineAsync("");
                        Console.WriteLine($"Asset '{assetName}' not found for ASSET connection.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling asset request: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the server and disconnects all clients.
        /// </summary>
        public void Stop()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();

                lock (_clientsLock)
                {
                    foreach (var client in _clients)
                    {
                        client.Close();
                    }
                    _clients.Clear();
                }
                Console.WriteLine("Server stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping server: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes the network server resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource.Dispose();
        }
    }
}
