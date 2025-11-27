using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Launcher.Services
{
    public class ServerManager
    {
        private const string ServersFilePath = "servers.json";
        public ObservableCollection<Server> Servers { get; }

        public ServerManager()
        {
            Servers = LoadServers();
        }

        private ObservableCollection<Server> LoadServers()
        {
            if (!File.Exists(ServersFilePath))
            {
                return new ObservableCollection<Server>();
            }

            try
            {
                var json = File.ReadAllText(ServersFilePath);
                var servers = JsonSerializer.Deserialize<List<Server>>(json);
                return new ObservableCollection<Server>(servers ?? new List<Server>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading servers: {ex.Message}");
                // In a real app, consider logging this error or showing a user-friendly message.
                return new ObservableCollection<Server>();
            }
        }

        public void SaveServers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Servers, options);
                File.WriteAllText(ServersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving servers: {ex.Message}");
                // Handle or log the error as appropriate.
            }
        }

        public void AddServer(Server server)
        {
            Servers.Add(server);
            SaveServers();
        }

        public void RemoveServer(Server server)
        {
            Servers.Remove(server);
            SaveServers();
        }
    }
}
