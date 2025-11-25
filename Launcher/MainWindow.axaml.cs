using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Server> _servers = new();
        private const string ServersFilePath = "servers.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadServers();
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
                Port = port
            };

            _servers.Add(server);
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
            }
        }
    }
}
