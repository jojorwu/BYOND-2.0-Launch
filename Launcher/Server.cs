using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Launcher
{
    public class Server : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _ipAddress = string.Empty;
        private int _port = 7777;
        private string _status = "Checking...";
        private long _ping = -1;
        private bool _isFavorite;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetField(ref _ipAddress, value);
        }

        public int Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public long Ping
        {
            get => _ping;
            set => SetField(ref _ping, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetField(ref _isFavorite, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
