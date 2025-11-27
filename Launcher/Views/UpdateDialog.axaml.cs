using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Launcher.Views
{
    public partial class UpdateDialog : Window
    {
        private bool _result = false;

        public UpdateDialog()
        {
            InitializeComponent();
            this.FindControl<Button>("DownloadButton").Click += (s, e) => { _result = true; Close(); };
            this.FindControl<Button>("LaterButton").Click += (s, e) => { _result = false; Close(); };
        }

        public static async Task<bool> Show(Window owner, string releaseName, string releaseNotes)
        {
            var dialog = new UpdateDialog();
            dialog.FindControl<TextBlock>("MessageTextBlock").Text = $"Новая версия: {releaseName}\n\n{releaseNotes}";
            await dialog.ShowDialog<bool>(owner);
            return dialog._result;
        }
    }
}
