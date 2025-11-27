using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Launcher.Views
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
            this.FindControl<Button>("OkButton").Click += (s, e) => Close();
        }

        public static Task Show(Window owner, string title, string message)
        {
            var dialog = new MessageBox
            {
                Title = title
            };
            dialog.FindControl<TextBlock>("MessageTextBlock").Text = message;
            return dialog.ShowDialog(owner);
        }
    }
}
