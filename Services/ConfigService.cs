using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkCountdown.Models;

namespace WorkCountdown.Services
{
    public static class ConfigService
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".workcountdown.json");

        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    return JsonSerializer.Deserialize<AppConfig>(json, _opts) ?? new();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[cfg] load: {ex.Message}"); }
            return new AppConfig();
        }

        public static void Save(AppConfig cfg)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(cfg, _opts)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[cfg] save: {ex.Message}"); }
        }
    }
}
