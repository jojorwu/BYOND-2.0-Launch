using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using Silk.NET.OpenGL;
using SilkWindow = Silk.NET.Windowing.Window;
using AvaloniaWindow = Avalonia.Controls.Window;

namespace Launcher
{
    public partial class MainWindow : AvaloniaWindow
    {
        private ObservableCollection<Server> _servers = new();
        private const string ServersFilePath = "servers.json";
        private bool _isInternalUpdate = false; // Флаг для предотвращения циклических обновлений

        public MainWindow()
        {
            InitializeComponent();

            // Подписки на изменение текста для авто-сохранения
            NameTextBox.TextChanged += ServerDetailsChanged;
            IpAddressTextBox.TextChanged += ServerDetailsChanged;
            PortTextBox.TextChanged += ServerDetailsChanged;

            LoadServers();
            CheckGlVersion();
        }

        private void CheckGlVersion()
        {
            Task.Run(() =>
            {
                var options = Silk.NET.Windowing.WindowOptions.Default;
                options.IsVisible = false;
                var glWindow = SilkWindow.Create(options);
                glWindow.Load += () =>
                {
                    var gl = GL.GetApi(glWindow);
                    var version = gl.GetStringS(StringName.Version);
                    glWindow.Close();

                    var match = Regex.Match(version, @"^(\d+)\.(\d+)");
                    if (match.Success && int.Parse(match.Groups[1].Value) >= 3 && int.Parse(match.Groups[2].Value) >= 3)
                    {
                        return;
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ShowError($"Your OpenGL version ({version}) is outdated. OpenGL 3.3+ required.");
                        ConnectButton.IsEnabled = false;
                    });
                };
                glWindow.Run(() => { });
            });
        }

        private void LoadServers()
        {
            if (File.Exists(ServersFilePath))
            {
                try
                {
                    var json = File.ReadAllText(ServersFilePath);
                    var serversList = JsonSerializer.Deserialize<List<Server>>(json) ?? new List<Server>();
                    _servers = new ObservableCollection<Server>(serversList);
                }
                catch
                {
                    _servers = new ObservableCollection<Server>();
                }
            }
            else
            {
                _servers = new ObservableCollection<Server>
                {
                    new Server { Name = "Local Dev Server", IpAddress = "127.0.0.1", Port = 7777, IsFavorite = true }
                };
                SaveServers();
            }

            ServerList.ItemsSource = _servers;
            SortServers();
        }

        private void SaveServers()
        {
            if (_isInternalUpdate) return;
            var json = JsonSerializer.Serialize(_servers);
            File.WriteAllText(ServersFilePath, json);
        }

        private void AddButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var server = new Server
            {
                Name = "New Server",
                IpAddress = "127.0.0.1",
                Port = 7777,
                IsFavorite = false
            };

            _servers.Add(server);
            ServerList.SelectedItem = server; // Автоматически выбираем новый сервер
            SaveServers();
        }

        private void DeleteButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                _servers.Remove(selectedServer);
                SaveServers();
                ConnectButton.IsEnabled = false;
            }
        }

        private void ConnectButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                try
                {
                    var launcherDir = AppContext.BaseDirectory;
                    // Корректный поиск клиента
                    var clientPath = Path.Combine(launcherDir, "Client", "Client");

                    if (OperatingSystem.IsWindows())
                    {
                        clientPath += ".exe";
                    }

                    // Если запускаем из студии/дебага, путь может отличаться
                    if (!File.Exists(clientPath))
                    {
                        var debugPath = Path.GetFullPath(Path.Combine(launcherDir, "..", "..", "..", "..", "Client", "bin", "Debug", "net8.0", "Client"));
                        if (OperatingSystem.IsWindows()) debugPath += ".exe";

                        if (File.Exists(debugPath)) clientPath = debugPath;
                    }

                    if (!File.Exists(clientPath))
                    {
                        ShowError($"Client executable not found at: {clientPath}");
                        return;
                    }

                    Process.Start(clientPath, $"{selectedServer.IpAddress} {selectedServer.Port}");
                    // Можно закрыть лаунчер после запуска, если нужно: Close();
                }
                catch (Exception ex)
                {
                    ShowError($"Error launching client: {ex.Message}");
                }
            }
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isInternalUpdate = true;
            if (ServerList.SelectedItem is Server selectedServer)
            {
                NameTextBox.Text = selectedServer.Name;
                IpAddressTextBox.Text = selectedServer.IpAddress;
                PortTextBox.Text = selectedServer.Port.ToString();
                FavoriteCheckBox.IsChecked = selectedServer.IsFavorite;
                ConnectButton.IsEnabled = true;
            }
            else
            {
                // Очистка полей если ничего не выбрано
                NameTextBox.Text = "";
                IpAddressTextBox.Text = "";
                PortTextBox.Text = "";
                FavoriteCheckBox.IsChecked = false;
                ConnectButton.IsEnabled = false;
            }
            _isInternalUpdate = false;
        }

        // Обновляем модель данных при изменении текста в полях
        private void ServerDetailsChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
        {
            if (_isInternalUpdate || ServerList.SelectedItem is not Server selectedServer) return;

            selectedServer.Name = NameTextBox.Text ?? "";
            selectedServer.IpAddress = IpAddressTextBox.Text ?? "";

            if (int.TryParse(PortTextBox.Text, out var port))
            {
                selectedServer.Port = port;
            }

            SaveServers();
        }

        private void FavoriteCheckBox_Changed(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_isInternalUpdate || ServerList.SelectedItem is not Server selectedServer) return;

            selectedServer.IsFavorite = FavoriteCheckBox.IsChecked ?? false;
            SortServers();
            SaveServers();
        }

        private void SortServers()
        {
            var selected = ServerList.SelectedItem;
            var sortedServers = _servers.OrderByDescending(s => s.IsFavorite).ToList();

            for (int newIndex = 0; newIndex < sortedServers.Count; newIndex++)
            {
                var serverToMove = sortedServers[newIndex];
                int oldIndex = _servers.IndexOf(serverToMove);

                if (oldIndex != newIndex)
                {
                    _servers.Move(oldIndex, newIndex);
                }
            }
            ServerList.SelectedItem = selected;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
            // Скрыть ошибку через 5 секунд
            Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ErrorTextBlock.IsVisible = false));
        }
    }
}