using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WorkCountdown.Models;
using WorkCountdown.Services;


namespace WorkCountdown.Windows;

public partial class MinimalWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly DispatcherTimer _timer;

    private bool _fwTriggered;
    private double _rStartW;
    private double _rStartX;

    public MinimalWindow(AppConfig cfg)
    {
        _cfg = cfg;
        InitializeComponent();

        Left = cfg.PosX;
        Top = cfg.PosY;
        Width = Math.Max(220, Math.Min(900, cfg.WinW));
        Topmost = cfg.AlwaysOnTop;
        Opacity = cfg.Opacity;

        ApplyFontSize();
        ApplyPctVisibility();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick();

        MouseRightButtonUp += (_, _) => ShowContextMenu();
    }


    private void Tick()
    {
        var state = CountdownService.GetState(_cfg);
        var now = DateTime.Now;

        string text;
        Color col;
        var accentC = HexColor(_cfg.EffectiveTimer);
        var warnC = HexColor(ThemeDefaults.Warn(_cfg.Theme));
        var okC = HexColor(ThemeDefaults.Ok(_cfg.Theme));
        var dimC = HexColor(ThemeDefaults.Dim(_cfg.Theme));

        switch (state.Status)
        {
            case WorkStatus.Before:
                text = CountdownService.FormatHms(state.Remaining);
                col = dimC; break;

            case WorkStatus.Working:
                text = CountdownService.FormatHms(state.Remaining);
                col = state.Remaining < 1800
                    ? (now.Second % 2 == 0 ? warnC : okC)
                    : accentC;
                break;

            default: // Done
                text = "00:00:00";
                col = okC;
                if (!_fwTriggered)
                {
                    _fwTriggered = true;
                    Dispatcher.BeginInvoke(LaunchFireworks);
                }
                break;
        }

        TimerLabel.Text = text;
        AnimateColor(TimerBrush, col);
        AnimateColor(BarBrush, col);
        AnimateColor(PctBrush, col);

        PctLabel.Text = state.Status == WorkStatus.Before
            ? "—"
            : $"{(int)(state.Progress * 100)}%";

        UpdateBar(state.Progress);
    }

    private void UpdateBar(double progress)
    {
        var totalW = ActualWidth - 28 - 44 - 8 - 8;   // margin + pct column
        if (totalW < 1) return;
        MiniBarFill.BeginAnimation(FrameworkElement.WidthProperty,
            new DoubleAnimation
            {
                To = Math.Max(0, totalW * progress),
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            });
    }

    private static void AnimateColor(SolidColorBrush brush, Color to)
        => brush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation { To = to, Duration = TimeSpan.FromMilliseconds(300) });


    private void ApplyFontSize()
        => TimerLabel.FontSize = Math.Clamp(Width / 6.0, 22, 86);

    private void ApplyPctVisibility()
        => PctPanel.Visibility = _cfg.MiniShowPct ? Visibility.Visible : Visibility.Collapsed;


    private void Panel_DragMove(object s, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
            _cfg.PosX = Left; _cfg.PosY = Top;
            ConfigService.Save(_cfg);
        }
    }


    private void ResizeGrip_DragDelta(object s, DragDeltaEventArgs e)
    {
        Width = Math.Max(220, Math.Min(900, Width + e.HorizontalChange));
        _cfg.WinW = Width;
        ApplyFontSize();
    }

    private void ResizeGrip_DragCompleted(object s, DragCompletedEventArgs e)
        => ConfigService.Save(_cfg);


    private void ShowContextMenu()
    {
        var menu = new ContextMenu();

        var sett = new MenuItem { Header = "⚙  Настройки" };
        sett.Click += (_, _) => OpenSettings();
        menu.Items.Add(sett);

        menu.Items.Add(new Separator());

        var aot = new MenuItem { Header = "Поверх всех окон", IsChecked = _cfg.AlwaysOnTop };
        aot.Click += (_, _) =>
        {
            _cfg.AlwaysOnTop = !_cfg.AlwaysOnTop;
            Topmost = _cfg.AlwaysOnTop;
            ConfigService.Save(_cfg);
        };
        menu.Items.Add(aot);

        var normal = new MenuItem { Header = "Обычный режим (с рамкой)" };
        normal.Click += (_, _) => SwitchToNormal();
        menu.Items.Add(normal);

        menu.Items.Add(new Separator());

        var fw = new MenuItem { Header = "🎇  Тест фейерверков" };
        fw.Click += (_, _) => LaunchFireworks();
        menu.Items.Add(fw);

        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "❌  Выход" };
        exit.Click += (_, _) => CloseApp();
        menu.Items.Add(exit);

        menu.IsOpen = true;
    }

    private void OpenSettings()
    {
        var sw = new SettingsWindow(_cfg) { Owner = this };
        if (sw.ShowDialog() == true)
        {
            ConfigService.Save(_cfg);
            _fwTriggered = false;
            Topmost = _cfg.AlwaysOnTop;
            Opacity = _cfg.Opacity;
            ApplyFontSize();
            ApplyPctVisibility();

            if (!_cfg.Minimalist) SwitchToNormal();
        }
    }

    private void SwitchToNormal()
    {
        _cfg.Minimalist = false;
        ConfigService.Save(_cfg);
        _timer.Stop();
        var mw = new MainWindow(_cfg);
        mw.Show();
        Close();
    }

    private void LaunchFireworks()
    {
        foreach (var m in MonitorService.GetAll())
        {
            var fw = new FireworksWindow();
            fw.Left = m.X; fw.Top = m.Y;
            fw.Width = m.Width; fw.Height = m.Height;
            fw.Show();
        }
    }

    private void CloseApp()
    {
        _cfg.PosX = Left; _cfg.PosY = Top;
        ConfigService.Save(_cfg);
        _timer.Stop();
        Application.Current.Shutdown();
    }


    private static Color HexColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.FromRgb(0x7C, 0x6A, 0xFF);
        try
        {
            hex = hex.TrimStart('#');
            return Color.FromRgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }
        catch { return Color.FromRgb(0x7C, 0x6A, 0xFF); }
    }
}