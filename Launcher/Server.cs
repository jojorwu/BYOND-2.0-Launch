using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Launcher
{
    public class Server : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string IpAddress { get; init; } = string.Empty;
        public int Port { get; init; }
        public bool IsFavorite { get; init; }

        private string _status = "Unknown";
        [JsonIgnore]
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
