using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkCountdown.Models;

namespace WorkCountdown.Services
{
    public static class CountdownService
    {
        public static CountdownState GetState(AppConfig cfg)
        {
            var now = DateTime.Now;
            var today = now.ToString("yyyy-MM-dd");

            DateTime start, end;

            if (cfg.CustomDate == today
                && !string.IsNullOrEmpty(cfg.CustomStart)
                && !string.IsNullOrEmpty(cfg.CustomEnd))
            {
                start = ParseTime(now, cfg.CustomStart!);
                end = ParseTime(now, cfg.CustomEnd!);
            }
            else
            {
                start = ParseTime(now, cfg.WorkStart);
                end = ParseTime(now, now.DayOfWeek == DayOfWeek.Friday
                    ? cfg.WorkEndFriday
                    : cfg.WorkEndWeekday);
            }

            double total = Math.Max(1, (end - start).TotalSeconds);

            if (now < start)
                return new()
                {
                    Status = WorkStatus.Before,
                    Remaining = (int)(end - now).TotalSeconds,
                    Progress = 0.0,
                    EndTime = end
                };

            if (now >= end)
                return new()
                {
                    Status = WorkStatus.Done,
                    Remaining = 0,
                    Progress = 1.0,
                    EndTime = end
                };

            double elapsed = (now - start).TotalSeconds;
            return new()
            {
                Status = WorkStatus.Working,
                Remaining = (int)(end - now).TotalSeconds,
                Progress = elapsed / total,
                EndTime = end,
            };
        }

        public static bool TryParseTime(string s, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m)) return false;
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            result = new TimeSpan(h, m, 0);
            return true;
        }

        public static string FormatHms(int totalSecs)
        {
            int h = totalSecs / 3600;
            int m = (totalSecs % 3600) / 60;
            int s = totalSecs % 60;
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        private static DateTime ParseTime(DateTime baseDate, string hhmm)
        {
            var parts = hhmm.Split(':');
            int h = int.Parse(parts[0]), mm = int.Parse(parts[1]);
            return baseDate.Date.AddHours(h).AddMinutes(mm);
        }
    }
}
