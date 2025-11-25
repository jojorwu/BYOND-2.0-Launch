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
        private ObservableCollection<Server> _servers;
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
                _servers = new ObservableCollection<Server>(JsonSerializer.Deserialize<List<Server>>(json));
            }
            else
            {
                _servers = new ObservableCollection<Server>();
            }

            this.FindControl<ListBox>("ServerList").ItemsSource = _servers;
        }

        private void SaveServers()
        {
            var json = JsonSerializer.Serialize(_servers);
            File.WriteAllText(ServersFilePath, json);
        }

        private void AddButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var server = new Server
            {
                Name = this.FindControl<TextBox>("NameTextBox").Text,
                IpAddress = this.FindControl<TextBox>("IpAddressTextBox").Text,
                Port = int.Parse(this.FindControl<TextBox>("PortTextBox").Text)
            };

            _servers.Add(server);
            SaveServers();
        }

        private void DeleteButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var selectedServer = (Server)this.FindControl<ListBox>("ServerList").SelectedItem;
            if (selectedServer != null)
            {
                _servers.Remove(selectedServer);
                SaveServers();
            }
        }

        private void ConnectButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var selectedServer = (Server)this.FindControl<ListBox>("ServerList").SelectedItem;
            if (selectedServer != null)
            {
                try
                {
                    var clientPath = Path.Combine("..", "Client", "bin", "Debug", "net8.0", "Client");
                    Process.Start(clientPath, $"{selectedServer.IpAddress} {selectedServer.Port}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start client: {ex.Message}");
                }
            }
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedServer = (Server)this.FindControl<ListBox>("ServerList").SelectedItem;
            if (selectedServer != null)
            {
                this.FindControl<TextBox>("NameTextBox").Text = selectedServer.Name;
                this.FindControl<TextBox>("IpAddressTextBox").Text = selectedServer.IpAddress;
                this.FindControl<TextBox>("PortTextBox").Text = selectedServer.Port.ToString();
            }
        }
    }
}
