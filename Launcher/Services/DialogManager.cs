using Avalonia.Controls;
using System.Threading.Tasks;

namespace Launcher.Services
{
    public static class DialogManager
    {
        public static async Task ShowMessageBox(Window owner, string title, string message)
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

            await messageBox.ShowDialog(owner);
        }

        public static async Task<bool> ShowConfirmationDialog(Window owner, string title, string message)
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

            await dialog.ShowDialog<bool>(owner);
            return result;
        }

        public static async Task<bool> ShowUpdateDialog(Window owner, string releaseName, string releaseNotes)
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

            await dialog.ShowDialog(owner);
            return result;
        }
    }
}
