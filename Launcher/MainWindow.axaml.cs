using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadServers();
            CheckGlVersion();
        }

        private void CheckGlVersion()
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
                    ErrorTextBlock.Text = $"Your system's OpenGL version ({version}) is not supported. Please update your graphics drivers to support at least OpenGL 3.3.";
                    ErrorTextBlock.IsVisible = true;
                    ConnectButton.IsEnabled = false;
                });
            };
            glWindow.Run(() => { });
        }

        private void LoadServers()
        {
            if (File.Exists(ServersFilePath))
            {
                var json = File.ReadAllText(ServersFilePath);
                var serversList = JsonSerializer.Deserialize<List<Server>>(json) ?? new List<Server>();
                _servers = new ObservableCollection<Server>(serversList);
            }
            else
            {
                _servers = new ObservableCollection<Server>();
            }

            ServerList.ItemsSource = _servers;
            SortServers();
        }

        private void SaveServers()
        {
            var json = JsonSerializer.Serialize(_servers);
            File.WriteAllText(ServersFilePath, json);
        }

        private void AddButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                Console.WriteLine("Invalid port number entered.");
                return;
            }

            var server = new Server
            {
                Name = NameTextBox.Text ?? string.Empty,
                IpAddress = IpAddressTextBox.Text ?? string.Empty,
                Port = port,
                IsFavorite = FavoriteCheckBox.IsChecked ?? false
            };

            _servers.Add(server);
            SortServers();
            SaveServers();
        }

        private void DeleteButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                _servers.Remove(selectedServer);
                SaveServers();
            }
        }

        private void ConnectButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                try
                {
                    var launcherDir = AppContext.BaseDirectory;
                    var clientPath = Path.Combine(launcherDir, "Client", "Client");

                    if (OperatingSystem.IsWindows())
                    {
                        clientPath += ".exe";
                    }

                    Process.Start(clientPath, $"{selectedServer.IpAddress} {selectedServer.Port}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error launching client: {ex.Message}");
                }
            }
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                NameTextBox.Text = selectedServer.Name;
                IpAddressTextBox.Text = selectedServer.IpAddress;
                PortTextBox.Text = selectedServer.Port.ToString();
                FavoriteCheckBox.IsChecked = selectedServer.IsFavorite;
            }
        }

        private void FavoriteCheckBox_Changed(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                selectedServer.IsFavorite = FavoriteCheckBox.IsChecked ?? false;
                SortServers();
                SaveServers();
            }
        }

        private void SortServers()
        {
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
        }
    }
}
