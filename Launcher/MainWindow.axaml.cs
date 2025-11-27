using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Octokit;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Server> _servers = new();
        private const string ServersFilePath = "servers.json";

        public MainWindow()
        {
            InitializeComponent();
            SetupEventHandlers();
            LoadServers();
        }

        private void SetupEventHandlers()
        {
            this.FindControl<Button>("RefreshAllButton").Click += RefreshAllButton_Click;
            this.FindControl<Button>("AddButton").Click += AddButton_Click;
            this.FindControl<Button>("EditButton").Click += EditButton_Click;
            this.FindControl<Button>("DeleteButton").Click += DeleteButton_Click;
            this.FindControl<Button>("ConnectButton").Click += ConnectButton_Click;
            this.FindControl<DataGrid>("ServerList").DoubleTapped += ConnectButton_Click; // Double-click to connect

            // Handle window loaded event to trigger initial refresh
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            await RefreshAllServersAsync();
            await CheckForUpdatesAsync();
        }

        private void LoadServers()
        {
            if (File.Exists(ServersFilePath))
            {
                try
                {
                    var json = File.ReadAllText(ServersFilePath);
                    var servers = JsonSerializer.Deserialize<List<Server>>(json);
                    _servers = new ObservableCollection<Server>(servers ?? new List<Server>());
                }
                catch (Exception ex)
                {
                    // Handle error or show message
                    Console.WriteLine($"Error loading servers: {ex.Message}");
                    _servers = new ObservableCollection<Server>();
                }
            }

            this.FindControl<DataGrid>("ServerList").ItemsSource = _servers;
        }

        private void SaveServers()
        {
            try
            {
                var json = JsonSerializer.Serialize(_servers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ServersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving servers: {ex.Message}");
                // Optionally show an error message to the user
            }
        }

        private async void RefreshAllButton_Click(object? sender, RoutedEventArgs e)
        {
            await RefreshAllServersAsync();
        }

        private async Task RefreshAllServersAsync()
        {
            var refreshButton = this.FindControl<Button>("RefreshAllButton");
            var statusText = this.FindControl<TextBlock>("StatusText");

            refreshButton.IsEnabled = false;
            statusText.Text = "Проверка статуса серверов...";

            var tasks = new List<Task>();
            foreach (var server in _servers)
            {
                tasks.Add(CheckServerStatusAsync(server));
            }
            await Task.WhenAll(tasks);

            statusText.Text = "Готово";
            refreshButton.IsEnabled = true;
        }

        private async Task CheckServerStatusAsync(Server server)
        {
            server.Status = "Проверка...";
            server.Ping = -1;

            using var tcpClient = new TcpClient();
            var stopwatch = new Stopwatch();

            try
            {
                var connectTask = tcpClient.ConnectAsync(server.IpAddress, server.Port);
                stopwatch.Start();

                if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask && !connectTask.IsFaulted)
                {
                    stopwatch.Stop();
                    server.Status = "Онлайн";
                    server.Ping = stopwatch.ElapsedMilliseconds;
                }
                else
                {
                    server.Status = "Офлайн";
                }
            }
            catch
            {
                server.Status = "Ошибка";
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditServerWindow();
            var result = await addWindow.ShowDialog<bool>(this);

            if (result)
            {
                _servers.Add(addWindow.Server);
                SaveServers();
                await CheckServerStatusAsync(addWindow.Server);
            }
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            var selectedServer = this.FindControl<DataGrid>("ServerList").SelectedItem as Server;
            if (selectedServer == null)
            {
                await ShowMessageBox("Ошибка", "Пожалуйста, выберите сервер для изменения.");
                return;
            }

            var editWindow = new AddEditServerWindow(new Server
            {
                Name = selectedServer.Name,
                IpAddress = selectedServer.IpAddress,
                Port = selectedServer.Port,
                IsFavorite = selectedServer.IsFavorite
            });

            var result = await editWindow.ShowDialog<bool>(this);

            if (result)
            {
                selectedServer.Name = editWindow.Server.Name;
                selectedServer.IpAddress = editWindow.Server.IpAddress;
                selectedServer.Port = editWindow.Server.Port;
                selectedServer.IsFavorite = editWindow.Server.IsFavorite;
                SaveServers();
                await CheckServerStatusAsync(selectedServer);
            }
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            var selectedServer = this.FindControl<DataGrid>("ServerList").SelectedItem as Server;
            if (selectedServer == null)
            {
                await ShowMessageBox("Ошибка", "Пожалуйста, выберите сервер для удаления.");
                return;
            }

            var confirm = await ShowConfirmationDialog("Подтверждение", $"Вы уверены, что хотите удалить сервер '{selectedServer.Name}'?");
            if (confirm)
            {
                _servers.Remove(selectedServer);
                SaveServers();
            }
        }

        private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
        {
            var selectedServer = this.FindControl<DataGrid>("ServerList").SelectedItem as Server;
            if (selectedServer == null)
            {
                await ShowMessageBox("Ошибка", "Пожалуйста, выберите сервер для подключения.");
                return;
            }

            if (selectedServer.Status != "Онлайн")
            {
                await ShowMessageBox("Ошибка", "Невозможно подключиться к серверу, который находится не в сети.");
                return;
            }

            try
            {
                var launcherDir = AppContext.BaseDirectory;
                // Path assumes Client is in a sibling directory to Launcher, e.g., /bin/Launcher/ and /bin/Client/
                var clientName = OperatingSystem.IsWindows() ? "Client.exe" : "Client";
                var clientPath = Path.Combine(launcherDir, "..", "Client", clientName);

                if (!File.Exists(clientPath))
                {
                     // Fallback for running from an IDE where the structure might be different
                    clientPath = Path.Combine(launcherDir, "..", "..", "..", "Client", "bin", "Debug", "net8.0", clientName);
                }

                if (!File.Exists(clientPath))
                {
                    await ShowMessageBox("Ошибка запуска", $"Не удалось найти исполняемый файл клиента по пути: {Path.GetFullPath(clientPath)}");
                    return;
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = $"{selectedServer.IpAddress} {selectedServer.Port}",
                    UseShellExecute = true // UseShellExecute is better for cross-platform compatibility
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Критическая ошибка", $"Не удалось запустить клиент: {ex.Message}");
            }
        }

        private async Task ShowMessageBox(string title, string message)
        {
            var messageBox = new Window
            {
                Title = title,
                Content = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20) },
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Margin = new Avalonia.Thickness(10) };
            okButton.Click += (s, e) => messageBox.Close();
            buttonPanel.Children.Add(okButton);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add((Control)messageBox.Content);
            messageBox.Content = mainPanel;

            await messageBox.ShowDialog(this);
        }

        private const string GhUser = "jojorwu";
        private const string GhRepo = "BYOND-2.0";

        private async Task CheckForUpdatesAsync()
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            statusText.Text = "Проверка обновлений...";

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("BYOND2-Launcher"));
                var releases = await github.Repository.Release.GetAll(GhUser, GhRepo);
                var latestRelease = releases.FirstOrDefault(r => !r.Prerelease);

                if (latestRelease == null)
                {
                    statusText.Text = "Не удалось найти релизы.";
                    return;
                }

                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var latestVersion = new Version(latestRelease.TagName.TrimStart('v'));

                if (latestVersion > assemblyVersion)
                {
                    statusText.Text = $"Доступна новая версия: {latestRelease.TagName}";
                    var download = await ShowUpdateDialog(latestRelease.Name, latestRelease.Body);
                    if (download)
                    {
                        var asset = latestRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                        if (asset != null)
                        {
                            await DownloadAndOpenFile(asset.BrowserDownloadUrl, asset.Name);
                        }
                        else
                        {
                            await ShowMessageBox("Ошибка обновления", "Не найден zip архив в последнем релизе.");
                        }
                    }
                }
                else
                {
                    statusText.Text = "У вас последняя версия.";
                }
            }
            catch (Exception ex)
            {
                statusText.Text = "Ошибка проверки обновлений.";
                Console.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private async Task<bool> ShowUpdateDialog(string releaseName, string releaseNotes)
        {
            var dialog = new Window
            {
                Title = "Доступно обновление!",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var textBlock = new TextBlock { Text = $"Новая версия: {releaseName}\n\n{releaseNotes}", Margin = new Avalonia.Thickness(15), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            var okButton = new Button { Content = "Скачать", Margin = new Avalonia.Thickness(5) };
            var cancelButton = new Button { Content = "Позже", Margin = new Avalonia.Thickness(5) };

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(10) };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(textBlock);

            dialog.Content = mainPanel;

            var result = false;
            okButton.Click += (s, e) => { result = true; dialog.Close(); };
            cancelButton.Click += (s, e) => { result = false; dialog.Close(); };

            await dialog.ShowDialog(this);
            return result;
        }

        private async Task DownloadAndOpenFile(string downloadUrl, string fileName)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            try
            {
                statusText.Text = $"Скачивание {fileName}...";
                using var client = new HttpClient();
                var data = await client.GetByteArrayAsync(downloadUrl);

                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
                await File.WriteAllBytesAsync(downloadsPath, data);

                statusText.Text = $"Файл сохранен в: {downloadsPath}";

                // Open the file or directory
                Process.Start(new ProcessStartInfo { FileName = downloadsPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                statusText.Text = "Ошибка скачивания.";
                await ShowMessageBox("Ошибка", $"Не удалось скачать файл: {ex.Message}");
            }
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var textBlock = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            var yesButton = new Button { Content = "Да", Margin = new Avalonia.Thickness(5) };
            var noButton = new Button { Content = "Нет", Margin = new Avalonia.Thickness(5) };

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(10) };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(textBlock);
            dialog.Content = mainPanel;

            var result = false;
            yesButton.Click += (s, e) => { result = true; dialog.Close(); };
            noButton.Click += (s, e) => { result = false; dialog.Close(); };

            await dialog.ShowDialog<bool>(this);
            return result;
        }
    }
}
