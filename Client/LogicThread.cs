using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using Core;

namespace Client
{
    public class LogicThread
    {
        public GameState PreviousState { get; private set; }
        public GameState CurrentState { get; private set; }

        private readonly object _lock = new object();
        private Thread _thread;
        private bool _isRunning;
        private readonly HashSet<string> _assetWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", // Images
            ".lua",                                 // Scripts
            ".json", ".xml", ".txt"                  // Data
        };
        public const int TicksPerSecond = 30;
        public const float TimeStep = 1.0f / TicksPerSecond;
        private int _nextId = 0;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public LogicThread(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            PreviousState = new GameState();
            CurrentState = new GameState();
            _thread = new Thread(GameLoop);
        }

        public void Start()
        {
            _isRunning = true;
            _thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _thread.Join();
            _stream?.Close();
            _tcpClient?.Close();
        }

        private async void GameLoop()
        {
            try
            {
                _tcpClient = new TcpClient(_serverIp, _serverPort);
                _stream = _tcpClient.GetStream();
                using (var writer = new StreamWriter(_stream, leaveOpen: true) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(NetworkConstants.PrimaryConnection);
                }
                Console.WriteLine("Connected to server with PRIMARY connection.");

                await HandleAssetTransfer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to server: {ex.Message}");
                return;
            }

            var stopwatch = new Stopwatch();
            double accumulator = 0;

            // Initialize game state
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var id = _nextId++;
                    CurrentState.Renderables.Add(id, new Core.Graphics.RenderableComponent
                    {
                        ID = id,
                        TextureID = 0,
                        Position = new System.Numerics.Vector2(x * 0.1f - 0.5f, y * 0.1f - 0.5f),
                        Rotation = 0,
                        Scale = new System.Numerics.Vector2(0.1f, 0.1f),
                        Color = new System.Numerics.Vector4((float)x / 10, (float)y / 10, 1.0f, 1.0f),
                        SourceRect = new System.Numerics.Vector4(0, 0, 1, 1)
                    });
                }
            }
            PreviousState = CurrentState.Clone();

            stopwatch.Start();
            double lastTime = stopwatch.Elapsed.TotalSeconds;

            while (_isRunning)
            {
                double currentTime = stopwatch.Elapsed.TotalSeconds;
                double frameTime = currentTime - lastTime;
                lastTime = currentTime;
                accumulator += frameTime;

                while (accumulator >= TimeStep)
                {
                    Update(TimeStep);
                    accumulator -= TimeStep;
                }
            }
        }

        private async Task HandleAssetTransfer()
        {
            try
            {
                using var reader = new StreamReader(_stream, leaveOpen: true);
                var assetList = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(assetList)) return;

                var filteredAssets = assetList.Split(',')
                    .Where(assetName => _assetWhitelist.Contains(Path.GetExtension(assetName)))
                    .ToList();

                foreach (var assetName in assetList.Split(',').Except(filteredAssets))
                {
                    Console.WriteLine($"Rejected asset with non-whitelisted extension: {assetName}");
                }

                var assetNames = new ConcurrentQueue<string>(filteredAssets);
                var serverAssetDir = Path.Combine(AppContext.BaseDirectory, "assets", $"{_serverIp}_{_serverPort}");
                Directory.CreateDirectory(serverAssetDir);

                const int numWorkers = 4;
                var workerTasks = new List<Task>();
                for (int i = 0; i < numWorkers; i++)
                {
                    workerTasks.Add(AssetDownloadWorker(assetNames, serverAssetDir));
                }

                await Task.WhenAll(workerTasks);

                Console.WriteLine("All assets downloaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in asset transfer: {ex.Message}");
            }
        }

        private async Task AssetDownloadWorker(ConcurrentQueue<string> assetNames, string serverAssetDir)
        {
            try
            {
                using var assetClient = new TcpClient(_serverIp, _serverPort);
                using var assetStream = assetClient.GetStream();
                using var assetWriter = new StreamWriter(assetStream, leaveOpen: true) { AutoFlush = true };

                await assetWriter.WriteLineAsync(NetworkConstants.AssetConnection);

                while (assetNames.TryDequeue(out var assetName))
                {
                    if (assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || assetName.Contains(".."))
                    {
                        Console.WriteLine($"Rejected asset with invalid name: {assetName}");
                        continue;
                    }

                    await assetWriter.WriteLineAsync(assetName);

                    var lengthBuffer = new byte[sizeof(long)];
                    await assetStream.ReadExactlyAsync(lengthBuffer, 0, sizeof(long));
                    var length = BitConverter.ToInt64(lengthBuffer, 0);

                    if (length == -1)
                    {
                        Console.WriteLine($"Asset '{assetName}' not found on server.");
                        continue;
                    }

                    var fileBytes = new byte[length];
                    await assetStream.ReadExactlyAsync(fileBytes, 0, (int)length);

                    var assetPath = Path.Combine(serverAssetDir, assetName);
                    await File.WriteAllBytesAsync(assetPath, fileBytes);
                    Console.WriteLine($"Downloaded asset: {assetPath}");
                }
                await assetWriter.WriteLineAsync(""); // Signal end of requests
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in asset download worker: {ex.Message}");
            }
        }

        private void Update(float deltaTime)
        {
            lock (_lock)
            {
                PreviousState = CurrentState.Clone();

                var newRenderables = new System.Collections.Generic.Dictionary<int, Core.Graphics.RenderableComponent>();
                foreach (var r in CurrentState.Renderables.Values)
                {
                    var newR = r;
                    newR.Position.X += 0.1f * deltaTime;
                    newR.Rotation += 0.5f * deltaTime;
                    if (newR.Position.X > 1.0f) newR.Position.X = -1.0f;
                    newRenderables[newR.ID] = newR;
                }
                CurrentState.Renderables = newRenderables;
                CurrentState.TickCount++;
            }
        }

        public (GameState, GameState) GetStatesForRender()
        {
            lock (_lock)
            {
                return (PreviousState, CurrentState);
            }
        }
    }
}
