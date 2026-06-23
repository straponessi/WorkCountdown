namespace WorkCountdown.Models
{
    /// <summary>Дефолтные цвета тем.</summary>
    public static class ThemeDefaults
    {
        public static string Bg(string theme) => theme == "light" ? "#F0F2FF" : "#0D0D1A";
        public static string Hdr(string theme) => theme == "light" ? "#E0E4FF" : "#13132A";
        public static string Accent(string theme) => theme == "light" ? "#5040D0" : "#7C6AFF";
        public static string Warn(string theme) => theme == "light" ? "#CC4422" : "#FF6B6B";
        public static string Ok(string theme) => theme == "light" ? "#007A6E" : "#4ECDC4";
        public static string Dim(string theme) => theme == "light" ? "#8080B0" : "#606090";
        public static string Border(string theme) => theme == "light" ? "#C0C4E8" : "#2A2A4A";
        public static string BarBg(string theme) => theme == "light" ? "#D0D4F0" : "#1E1E38";
    }
}
