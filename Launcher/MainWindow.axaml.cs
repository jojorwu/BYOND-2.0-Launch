using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Launcher.Services;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private readonly ServerManager _serverManager;
        private readonly ServerPinger _serverPinger;
        private readonly UpdateService _updateService;

        public ObservableCollection<Server> Servers { get; set; }
        public ObservableCollection<Server> FilteredServers { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _serverManager = new ServerManager();
            _serverPinger = new ServerPinger();
            _updateService = new UpdateService();

            Servers = new ObservableCollection<Server>(_serverManager.Servers);
            FilteredServers = new ObservableCollection<Server>(Servers);

            DataContext = this;

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            this.Loaded += MainWindow_Loaded;
            this.FindControl<Button>("RefreshAllButton").Click += RefreshAllButton_Click;
            this.FindControl<Button>("AddButton").Click += AddButton_Click;
            this.FindControl<Button>("EditButton").Click += EditButton_Click;
            this.FindControl<Button>("DeleteButton").Click += DeleteButton_Click;
            this.FindControl<Button>("ConnectButton").Click += ConnectButton_Click;
            this.FindControl<DataGrid>("ServerList").DoubleTapped += ConnectButton_Click;

            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox != null) searchBox.TextChanged += (s, e) => ApplyFilter();

            var favoritesFilter = this.FindControl<CheckBox>("FavoritesFilter");
            if (favoritesFilter != null) favoritesFilter.IsCheckedChanged += (s, e) => ApplyFilter();
        }

        private void ApplyFilter()
        {
            var searchBox = this.FindControl<TextBox>("SearchBox");
            var favoritesFilter = this.FindControl<CheckBox>("FavoritesFilter");

            var searchText = searchBox?.Text?.ToLower() ?? string.Empty;
            var favoritesOnly = favoritesFilter?.IsChecked ?? false;

            var filtered = Servers
                .Where(s => (s.Name.ToLower().Contains(searchText) || s.IpAddress.ToLower().Contains(searchText)) &&
                            (!favoritesOnly || s.IsFavorite))
                .ToList();

            FilteredServers.Clear();
            foreach (var server in filtered)
            {
                FilteredServers.Add(server);
            }
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            await RefreshAllServersAsync();
            await CheckForUpdatesAsync();
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
            foreach (var server in Servers)
            {
                tasks.Add(_serverPinger.CheckServerStatusAsync(server));
            }
            await Task.WhenAll(tasks);

            ApplyFilter();
            statusText.Text = "Готово";
            refreshButton.IsEnabled = true;
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditServerWindow();
            var result = await addWindow.ShowDialog<bool>(this);

            if (result)
            {
                _serverManager.AddServer(addWindow.Server);
                Servers.Add(addWindow.Server);
                await _serverPinger.CheckServerStatusAsync(addWindow.Server);
                ApplyFilter();
            }
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (this.FindControl<DataGrid>("ServerList").SelectedItem is not Server selectedServer)
            {
                await DialogManager.ShowMessageBox(this, "Ошибка", "Пожалуйста, выберите сервер для изменения.");
                return;
            }

            var editWindow = new AddEditServerWindow(new Server
            {
                Name = selectedServer.Name,
                IpAddress = selectedServer.IpAddress,
                Port = selectedServer.Port,
                IsFavorite = selectedServer.IsFavorite,
                Timeout = selectedServer.Timeout
            });

            var result = await editWindow.ShowDialog<bool>(this);

            if (result)
            {
                selectedServer.Name = editWindow.Server.Name;
                selectedServer.IpAddress = editWindow.Server.IpAddress;
                selectedServer.Port = editWindow.Server.Port;
                selectedServer.IsFavorite = editWindow.Server.IsFavorite;
                selectedServer.Timeout = editWindow.Server.Timeout;
                _serverManager.SaveServers();
                await _serverPinger.CheckServerStatusAsync(selectedServer);
                ApplyFilter();
            }
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (this.FindControl<DataGrid>("ServerList").SelectedItem is not Server selectedServer)
            {
                await DialogManager.ShowMessageBox(this, "Ошибка", "Пожалуйста, выберите сервер для удаления.");
                return;
            }

            var confirm = await DialogManager.ShowConfirmationDialog(this, "Подтверждение", $"Вы уверены, что хотите удалить сервер '{selectedServer.Name}'?");
            if (confirm)
            {
                _serverManager.RemoveServer(selectedServer);
                Servers.Remove(selectedServer);
                ApplyFilter();
            }
        }

        private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (this.FindControl<DataGrid>("ServerList").SelectedItem is not Server selectedServer)
            {
                await DialogManager.ShowMessageBox(this, "Ошибка", "Пожалуйста, выберите сервер для подключения.");
                return;
            }

            if (selectedServer.Status != "Онлайн")
            {
                await DialogManager.ShowMessageBox(this, "Ошибка", "Невозможно подключиться к серверу, который находится не в сети.");
                return;
            }

            try
            {
                var clientName = OperatingSystem.IsWindows() ? "Client.exe" : "Client";
                var clientPath = Path.Combine(AppContext.BaseDirectory, "..", "Client", clientName);

                if (!File.Exists(clientPath))
                {
                    await DialogManager.ShowMessageBox(this, "Ошибка запуска", $"Не удалось найти исполняемый файл клиента: {Path.GetFullPath(clientPath)}");
                    return;
                }

                Process.Start(new ProcessStartInfo { FileName = clientPath, Arguments = $"{selectedServer.IpAddress} {selectedServer.Port}", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                await DialogManager.ShowMessageBox(this, "Критическая ошибка", $"Не удалось запустить клиент: {ex.Message}");
            }
        }

        private void FavoriteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: Server selectedServer })
            {
                selectedServer.IsFavorite = !selectedServer.IsFavorite;
                _serverManager.SaveServers();
                ApplyFilter();
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            statusText.Text = "Проверка обновлений...";

            if (await _updateService.CheckForUpdatesAsync() && _updateService.LatestRelease != null)
            {
                var release = _updateService.LatestRelease;
                statusText.Text = $"Доступна новая версия: {release.TagName}";
                var download = await DialogManager.ShowUpdateDialog(this, release.Name, release.Body);
                if (download)
                {
                    var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                    if (asset != null)
                    {
                        await DownloadAndOpenFile(asset.BrowserDownloadUrl, asset.Name);
                    }
                    else
                    {
                        await DialogManager.ShowMessageBox(this, "Ошибка обновления", "Не найден zip архив в последнем релизе.");
                    }
                }
            }
            else
            {
                statusText.Text = "У вас последняя версия.";
            }
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

                Process.Start(new ProcessStartInfo { FileName = downloadsPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                statusText.Text = "Ошибка скачивания.";
                await DialogManager.ShowMessageBox(this, "Ошибка", $"Не удалось скачать файл: {ex.Message}");
            }
        }
    }
}
