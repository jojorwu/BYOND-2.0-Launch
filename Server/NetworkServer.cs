using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class NetworkServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly object _clientsLock = new object();

        public NetworkServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _cancellationTokenSource = new CancellationTokenSource();
        }

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
            catch (OperationCanceledException) { }
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
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
                Directory.CreateDirectory(assetsDir);

                var assetFiles = Directory.GetFiles(assetsDir);
                var assetNames = new List<string>();
                foreach (var file in assetFiles)
                {
                    assetNames.Add(Path.GetFileName(file));
                }

                await writer.WriteLineAsync(string.Join(",", assetNames));

                while (client.Connected)
                {
                    var requestedFile = await reader.ReadLineAsync();
                    if (requestedFile == null || requestedFile == "done")
                    {
                        break;
                    }

                    var safeFileName = Path.GetFileName(requestedFile);
                    var filePath = Path.Combine(assetsDir, safeFileName);

                    if (File.Exists(filePath))
                    {
                        var fileBytes = await File.ReadAllBytesAsync(filePath);
                        var base64Content = Convert.ToBase64String(fileBytes);
                        await writer.WriteLineAsync(base64Content);
                    }
                    else
                    {
                        await writer.WriteLineAsync("error:not_found");
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine("Client disconnected (SocketException).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during client communication: {ex.Message}");
            }
            finally
            {
                client.Close();
                lock(_clientsLock)
                {
                    _clients.Remove(client);
                }
            }
        }

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

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource.Dispose();
        }
    }
}
