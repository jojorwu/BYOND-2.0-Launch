using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Server> _servers = new();
        private const string ServersFilePath = "servers.json";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await LoadServersAsync();
            RefreshButton_Click(this, new Avalonia.Interactivity.RoutedEventArgs());
        }

        private async Task LoadServersAsync()
        {
            if (File.Exists(ServersFilePath))
            {
                var json = await File.ReadAllTextAsync(ServersFilePath);
                var serversList = JsonSerializer.Deserialize<List<Server>>(json) ?? new List<Server>();
                _servers = new ObservableCollection<Server>(serversList);
            }
            else
            {
                _servers = new ObservableCollection<Server>();
            }

            ServerList.ItemsSource = _servers;
        }

        private async void RefreshButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            StatusTextBlock.Text = "Refreshing server statuses...";
            var tasks = new List<Task>();
            foreach (var server in _servers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    server.Status = "Pinging...";
                    server.Status = await PingServer(server.IpAddress, server.Port) ? "Online" : "Offline";
                }));
            }
            await Task.WhenAll(tasks);
            StatusTextBlock.Text = "Refresh complete.";
        }


        private async void ConnectButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                ConnectButton.IsEnabled = false;
                StatusTextBlock.Text = "Pinging...";

                if (await PingServer(selectedServer.IpAddress, selectedServer.Port))
                {
                    StatusTextBlock.Text = "Connecting...";
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
                        StatusTextBlock.Text = $"Error launching client: {ex.Message}";
                    }
                }
                else
                {
                    StatusTextBlock.Text = "Server is offline.";
                }

                ConnectButton.IsEnabled = true;
            }
            else
            {
                StatusTextBlock.Text = "Please select a server to connect to.";
            }
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Method is now empty, but kept for future use.
        }

        private async Task<bool> PingServer(string ipAddress, int port)
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync(ipAddress, port);
                if (await Task.WhenAny(task, Task.Delay(2000)) == task)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Ignore exceptions
            }

            return false;
        }
    }
}
