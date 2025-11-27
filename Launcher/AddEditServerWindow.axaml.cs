using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher
{
    public partial class AddEditServerWindow : Window
    {
        public Server Server { get; private set; }

        public AddEditServerWindow(Server server = null)
        {
            InitializeComponent();
            Server = server ?? new Server();
            DataContext = Server; // This is for potential future binding, not strictly needed now

            this.FindControl<TextBox>("NameTextBox").Text = Server.Name;
            this.FindControl<TextBox>("IpAddressTextBox").Text = Server.IpAddress;
            this.FindControl<NumericUpDown>("PortUpDown").Value = Server.Port;
            this.FindControl<CheckBox>("FavoriteCheckBox").IsChecked = Server.IsFavorite;

            this.FindControl<Button>("OkButton").Click += OkButton_Click;
            this.FindControl<Button>("CancelButton").Click += CancelButton_Click;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Server.Name = this.FindControl<TextBox>("NameTextBox").Text;
            Server.IpAddress = this.FindControl<TextBox>("IpAddressTextBox").Text;
            Server.Port = (int)this.FindControl<NumericUpDown>("PortUpDown").Value;
            Server.IsFavorite = this.FindControl<CheckBox>("FavoriteCheckBox").IsChecked ?? false;

            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
