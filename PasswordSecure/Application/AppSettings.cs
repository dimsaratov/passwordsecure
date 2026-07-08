using System.ComponentModel;

using PasswordGenerator;

namespace PasswordSecure.Application
{
    public class AppSettings : INotifyPropertyChanged
    {
        public string? LastFile { get; set; }

        public double WindowWidth { get; set; } = 600;

        public GenerationSettings GenerationSettings { get; set; } = new();

        public int TimeSafePassword { get; set; } = 30;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
