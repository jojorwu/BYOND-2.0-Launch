using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Launcher.Views
{
    public partial class ConfirmationDialog : Window
    {
        private bool _result = false;

        public ConfirmationDialog()
        {
            InitializeComponent();
            this.FindControl<Button>("YesButton").Click += (s, e) => { _result = true; Close(); };
            this.FindControl<Button>("NoButton").Click += (s, e) => { _result = false; Close(); };
        }

        public static async Task<bool> Show(Window owner, string title, string message)
        {
            var dialog = new ConfirmationDialog
            {
                Title = title
            };
            dialog.FindControl<TextBlock>("MessageTextBlock").Text = message;
            await dialog.ShowDialog<bool>(owner);
            return dialog._result;
        }
    }
}
