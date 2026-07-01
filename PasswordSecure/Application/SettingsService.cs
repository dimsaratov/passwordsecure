using System;
using System.IO;
using System.Text.Json;

namespace PasswordSecure.Application
{
    public static class SettingsService
    {
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PasswordSecure",
            "settings.json"
        );

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings? settings)
        {
            settings ??= new AppSettings();

            string? directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsPath, json);
        }
    }
}