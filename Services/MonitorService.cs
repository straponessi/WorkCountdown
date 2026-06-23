namespace WorkCountdown.Services
{
    public record MonitorRect(int X, int Y, int Width, int Height);

    public static class MonitorService
    {
        public static IReadOnlyList<MonitorRect> GetAll()
        {
            var result = new List<MonitorRect>();
            foreach (var s in System.Windows.Forms.Screen.AllScreens)
            {
                result.Add(new MonitorRect(
                    s.Bounds.X, s.Bounds.Y,
                    s.Bounds.Width, s.Bounds.Height));
            }
            return result.Count > 0 ? result : [new MonitorRect(0, 0, 1920, 1080)];
        }
    }
}
