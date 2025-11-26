using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    public class LogicThread
    {
        public GameState PreviousState { get; private set; }
        public GameState CurrentState { get; private set; }

        private readonly object _lock = new object();
        private Thread _thread;
        private bool _isRunning;
        public const int TicksPerSecond = 30;
        public const float TimeStep = 1.0f / TicksPerSecond;
        private int _nextId = 0;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private ConnectionState _connectionState = ConnectionState.Disconnected;

        public bool IsConnected => _connectionState == ConnectionState.Connected;

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

        private async Task ConnectWithRetriesAsync()
        {
            _connectionState = ConnectionState.Connecting;
            while (_isRunning && !IsConnected)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(_serverIp, _serverPort);
                    _stream = _tcpClient.GetStream();
                    _connectionState = ConnectionState.Connected;
                    Console.WriteLine("Connected to server.");
                    HandleAssetTransfer();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to server: {ex.Message}. Retrying in 5 seconds...");
                    await Task.Delay(5000);
                }
            }
        }

        private async void GameLoop()
        {
            await ConnectWithRetriesAsync();

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
                if (!IsConnected)
                {
                    await ConnectWithRetriesAsync();
                }

                RunGameSimulation(stopwatch, ref lastTime, ref accumulator);
            }
        }

        private void RunGameSimulation(Stopwatch stopwatch, ref double lastTime, ref double accumulator)
        {
            double currentTime = stopwatch.Elapsed.TotalSeconds;
            double frameTime = currentTime - lastTime;
            lastTime = currentTime;
            accumulator += frameTime;

            while (accumulator >= TimeStep)
            {
                try
                {
                    Update(TimeStep);
                }
                catch (IOException)
                {
                    Console.WriteLine("Connection lost. Reconnecting...");
                    _connectionState = ConnectionState.Disconnected;
                }
                accumulator -= TimeStep;
            }
        }

        private void HandleAssetTransfer()
        {
            if (_stream == null)
            {
                return;
            }

            try
            {
                using (var reader = new StreamReader(_stream, leaveOpen: true))
                using (var writer = new StreamWriter(_stream, leaveOpen: true) { AutoFlush = true })
                {
                    writer.WriteLine("LIST_ASSETS");
                    var assetList = reader.ReadLine();
                    if (string.IsNullOrEmpty(assetList)) return;

                    var assetNames = assetList.Split(',');
                    var serverAssetDir = Path.Combine(AppContext.BaseDirectory, "assets", $"{_serverIp}_{_serverPort}");
                    Directory.CreateDirectory(serverAssetDir);

                    foreach (var assetName in assetNames)
                    {
                        if (!IsValidAssetName(assetName))
                        {
                            Console.WriteLine($"Server sent invalid asset name: '{assetName}'. Skipping.");
                            continue;
                        }

                        writer.WriteLine($"DOWNLOAD {assetName}");
                        var lengthStr = reader.ReadLine();
                        if (int.TryParse(lengthStr, out int length) && length > 0)
                        {
                            var buffer = new byte[length];
                            _stream.Read(buffer, 0, length);
                            var assetPath = Path.Combine(serverAssetDir, assetName);
                            File.WriteAllBytes(assetPath, buffer);
                            Console.WriteLine($"Downloaded asset: {assetPath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in asset transfer: {ex.Message}");
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
