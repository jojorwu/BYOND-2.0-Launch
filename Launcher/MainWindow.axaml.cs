using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            SortServers();
        }

        private void SaveServers()
        {
            SortServers();
            var json = JsonSerializer.Serialize(_servers);
            File.WriteAllText(ServersFilePath, json);
        }

        private void SortServers()
        {
            var sortedServers = _servers.OrderByDescending(s => s.IsFavorite).ToList();
            for (int i = 0; i < sortedServers.Count; i++)
            {
                var server = sortedServers[i];
                var oldIndex = _servers.IndexOf(server);
                if (oldIndex != i)
                {
                    _servers.Move(oldIndex, i);
                }
            }
        }

        private void AddButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                StatusTextBlock.Text = "Server name cannot be empty.";
                return;
            }

            if (string.IsNullOrWhiteSpace(IpAddressTextBox.Text))
            {
                StatusTextBlock.Text = "IP address cannot be empty.";
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                StatusTextBlock.Text = "Invalid port number.";
                return;
            }

            if (ServerList.SelectedItem is Server selectedServer)
            {
                UpdateSelectedServer(selectedServer, port);
            }
            else
            {
                AddNewServer(port);
            }

            SaveServers();
        }

        private void AddNewServer(int port)
        {
            var server = new Server
            {
                Name = NameTextBox.Text,
                IpAddress = IpAddressTextBox.Text,
                Port = port,
                IsFavorite = FavoriteCheckBox.IsChecked ?? false
            };
            _servers.Add(server);
            StatusTextBlock.Text = "Server added successfully.";
            ClearInputFields();
        }

        private void UpdateSelectedServer(Server server, int port)
        {
            server.Name = NameTextBox.Text;
            server.IpAddress = IpAddressTextBox.Text;
            server.Port = port;
            server.IsFavorite = FavoriteCheckBox.IsChecked ?? false;
            SortServers();
            StatusTextBlock.Text = "Server updated successfully.";
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

        private void NewServerButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ServerList.SelectedItem = null;
        }

        private void ClearInputFields()
        {
            NameTextBox.Text = string.Empty;
            IpAddressTextBox.Text = string.Empty;
            PortTextBox.Text = string.Empty;
            FavoriteCheckBox.IsChecked = false;
        }

        private void DeleteButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                _servers.Remove(selectedServer);
                SaveServers();
                StatusTextBlock.Text = "Server deleted successfully.";
                ServerList.SelectedItem = null;
                ClearInputFields();
            }
            else
            {
                StatusTextBlock.Text = "Please select a server to delete.";
            }
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
            if (ServerList.SelectedItem is Server selectedServer)
            {
                NameTextBox.Text = selectedServer.Name;
                IpAddressTextBox.Text = selectedServer.IpAddress;
                PortTextBox.Text = selectedServer.Port.ToString();
                FavoriteCheckBox.IsChecked = selectedServer.IsFavorite;
                AddButton.Content = "Update";
            }
            else
            {
                AddButton.Content = "Add";
                ClearInputFields();
            }
        }

        private void FavoriteCheckBox_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ServerList.SelectedItem is Server selectedServer)
            {
                selectedServer.IsFavorite = FavoriteCheckBox.IsChecked ?? false;
                SaveServers();
            }
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
