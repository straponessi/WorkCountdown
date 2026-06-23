using System.Text.Json.Serialization;


namespace WorkCountdown.Models
{
    public class AppConfig
    {
        public string WorkStart { get; set; } = "09:00";
        public string WorkEndWeekday { get; set; } = "18:00";
        public string WorkEndFriday { get; set; } = "17:00";

        public string? CustomStart { get; set; }
        public string? CustomEnd { get; set; }
        public string? CustomDate { get; set; }

        public double PosX { get; set; } = 60;
        public double PosY { get; set; } = 60;
        public double WinW { get; set; } = 310;

        public double Opacity { get; set; } = 0.92;
        public string Theme { get; set; } = "dark";

        public bool AlwaysOnTop { get; set; } = false;
        public bool Minimalist { get; set; } = false;
        public bool MiniShowPct { get; set; } = true;
        public bool Autostart { get; set; } = false;

        public string? ColorBg { get; set; }   
        public string? ColorTimer { get; set; }  

        [JsonIgnore] public string EffectiveBg => ColorBg ?? ThemeDefaults.Bg(Theme);
        [JsonIgnore] public string EffectiveTimer => ColorTimer ?? ThemeDefaults.Accent(Theme);
    }
}
