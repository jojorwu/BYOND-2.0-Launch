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
            lock (_clientsLock)
            {
                _clients.Add(client);
            }
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            Task.Run(() => HandleClientCommunication(client));
        }

        private async Task HandleClientCommunication(TcpClient client)
        {
            try
            {
                await SendAssetList(client);
                await ReceiveAssetRequests(client);
            }
            finally
            {
                RemoveClient(client);
            }
        }

        private void RemoveClient(TcpClient client)
        {
            lock (_clientsLock)
            {
                _clients.Remove(client);
            }
            client.Close();
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
        }

        private async Task SendAssetList(TcpClient client)
        {
            try
            {
                var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
                if (!Directory.Exists(assetsDir))
                {
                    Directory.CreateDirectory(assetsDir);
                }

                var assetFiles = Directory.GetFiles(assetsDir);
                var assetNames = new List<string>();
                foreach (var file in assetFiles)
                {
                    assetNames.Add(Path.GetFileName(file));
                }

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

        private async Task ReceiveAssetRequests(TcpClient client)
        {
            try
            {
                using (var reader = new StreamReader(client.GetStream(), leaveOpen: true))
                using (var writer = new StreamWriter(client.GetStream(), leaveOpen: true) { AutoFlush = true })
                {
                    while (client.Connected)
                    {
                        var assetName = await reader.ReadLineAsync();
                        if (assetName == null) break;

                        if (!IsValidAssetName(assetName))
                        {
                            Console.WriteLine($"Rejected invalid asset request: '{assetName}'");
                            await writer.WriteLineAsync("0");
                            continue;
                        }

                        var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
                        var assetPath = Path.Combine(assetsDir, assetName);

                        if (!Path.GetFullPath(assetPath).StartsWith(Path.GetFullPath(assetsDir)))
                        {
                            Console.WriteLine($"Rejected path traversal attempt: '{assetName}'");
                            await writer.WriteLineAsync("0");
                            continue;
                        }

                        if (File.Exists(assetPath))
                        {
                            var fileBytes = await File.ReadAllBytesAsync(assetPath);
                            await writer.WriteLineAsync(fileBytes.Length.ToString());
                            await client.GetStream().WriteAsync(fileBytes, 0, fileBytes.Length);
                            Console.WriteLine($"Sent asset '{assetName}' to client.");
                        }
                        else
                        {
                            await writer.WriteLineAsync("0");
                            Console.WriteLine($"Asset '{assetName}' not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving asset requests: {ex.Message}");
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

        private bool IsValidAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            if (assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            if (assetName.Contains(".."))
            {
                return false;
            }

            return true;
        }
    }
}
