using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkCountdown.Models;

namespace WorkCountdown.Services
{
    public static class AutostartService
    {
        private const string AppName = "WorkCountdown";
        private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath);
                return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }

        public static void Set(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath, writable: true)!;
                if (enable)
                {
                    var exe = Environment.ProcessPath
                        ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{exe}\"");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[autostart] {ex.Message}"); }
        }
    }
}
