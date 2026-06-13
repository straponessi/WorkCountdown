using System.Windows;
using WorkCountdown.Services;
using WorkCountdown.Windows;

namespace WorkCountdown;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var cfg = ConfigService.Load();

        if (cfg.Minimalist)
        {
            var mw = new MinimalWindow(cfg);
            mw.Show();
        }
        else
        {
            var mw = new MainWindow(cfg);
            mw.Show();
        }
    }
}