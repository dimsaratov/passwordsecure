using PasswordGenerator;

namespace PasswordSecure.Application
{
    public class AppSettings
    {
        public string? LastFile { get; set; }
        public double WindowWidth { get; set; } = 600;
        public GenerationSettings GenerationSettings { get; set; } = new();

    }
}
