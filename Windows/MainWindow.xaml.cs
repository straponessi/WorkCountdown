using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WorkCountdown.Models;
using WorkCountdown.Services;

namespace WorkCountdown.Windows;

public partial class MainWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _timer;

    private bool _resizing;
    private double _startW;
    private double _resizeStartX;

    public MainWindow(AppConfig cfg)
    {
        _cfg = cfg;
        _vm = new MainViewModel(cfg);
        _vm.WorkDayDone += (_, _) => Dispatcher.BeginInvoke(LaunchFireworks);

        InitializeComponent();
        DataContext = _vm;

        ApplyTheme();
        RestorePosition();
        ApplyOpacity();

        Topmost = cfg.AlwaysOnTop;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        Tick(); // немедленный первый тик
    }


    private void Tick()
    {
        _vm.Tick();
        UpdateTimerColor();
        UpdateProgressBar();
    }

    private void UpdateTimerColor()
    {
        var anim = new ColorAnimation
        {
            To = _vm.TimerColor,
            Duration = TimeSpan.FromMilliseconds(300),
        };
        TimerBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        BarBrush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation { To = _vm.BarColor, Duration = TimeSpan.FromMilliseconds(300) });
    }

    private void UpdateProgressBar()
    {
        double totalW = BodyBorder.ActualWidth - 32; // margin 16*2
        if (totalW < 1) return;
        var anim = new DoubleAnimation
        {
            To = totalW * _vm.Progress,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        BarFill.BeginAnimation(FrameworkElement.WidthProperty, anim);
    }


    private void ApplyTheme()
    {
        string t = _cfg.Theme;
        RootBorder.Background = Brush(ThemeDefaults.Border(t));
        HeaderBorder.Background = Brush(_cfg.ColorBg ?? ThemeDefaults.Hdr(t));
        BodyBorder.Background = Brush(_cfg.ColorBg ?? ThemeDefaults.Bg(t));
        GripBorder.Background = Brush(_cfg.ColorBg ?? ThemeDefaults.Bg(t));

        var dimBrush = Brush(ThemeDefaults.Dim(t));
        var barBgBrush = Brush(ThemeDefaults.BarBg(t));

        AppTitleLabel.Foreground = dimBrush;
        DayLabel.Foreground = dimBrush;
        PctLabel.Foreground = dimBrush;
        StatusLabel.Foreground = dimBrush;
        BarBg.Background = barBgBrush;

        TimerBrush.Color = HexColor(_cfg.EffectiveTimer);
        BarBrush.Color = HexColor(_cfg.EffectiveTimer);

        // Кнопки хедера
        SettingsBtn.Foreground = dimBrush;
        CloseBtn.Foreground = dimBrush;

        // Ширина
        Width = Math.Max(200, Math.Min(900, _cfg.WinW));
    }

    private void ApplyOpacity() => Opacity = _cfg.Opacity;

    private void RestorePosition()
    {
        Left = Math.Max(0, _cfg.PosX);
        Top = Math.Max(0, _cfg.PosY);
    }

    private static SolidColorBrush Brush(string hex) =>
        new(HexColor(hex));

    // Исправлено: использование System.Windows.Media.Color вместо неоднозначного Color
    // (было: private static Color HexColor(string? hex))
    private static System.Windows.Media.Color HexColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return Colors.Transparent;
        try
        {
            hex = hex.TrimStart('#');
            // Исправлено: System.Windows.Media.Color.FromRgb вместо Color.FromRgb
            // (было: return Color.FromRgb(...))
            return System.Windows.Media.Color.FromRgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }
        catch { return Colors.Transparent; }
    }


    private void Header_DragMove(object s, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        _cfg.PosX = Left; _cfg.PosY = Top;
    }


    private void ResizeGrip_DragDelta(object s, DragDeltaEventArgs e)
    {
        double newW = Math.Max(200, Math.Min(900, Width + e.HorizontalChange));
        Width = newW;
        _cfg.WinW = newW;
        // Обновляем размер шрифта таймера
        TimerLabel.FontSize = Math.Clamp(newW / 7.0, 22, 72);
    }

    private void ResizeGrip_DragCompleted(object s, DragCompletedEventArgs e)
        => ConfigService.Save(_cfg);


    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        ShowContextMenu();
    }

    private void ShowContextMenu()
    {
        var menu = new ContextMenu();

        var sett = new MenuItem { Header = "⚙  Настройки" };
        sett.Click += (_, _) => Settings_Click(null!, null!);
        menu.Items.Add(sett);

        menu.Items.Add(new Separator());

        var aot = new MenuItem
        {
            Header = "Поверх всех окон",
            IsChecked = _cfg.AlwaysOnTop,
        };
        aot.Click += (_, _) =>
        {
            _cfg.AlwaysOnTop = !_cfg.AlwaysOnTop;
            Topmost = _cfg.AlwaysOnTop;
            ConfigService.Save(_cfg);
        };
        menu.Items.Add(aot);

        var mini = new MenuItem { Header = "Минималистичный режим" };
        mini.Click += (_, _) => SwitchToMinimal();
        menu.Items.Add(mini);

        menu.Items.Add(new Separator());

        var fw = new MenuItem { Header = "🎇  Тест фейерверков (все мониторы)" };
        fw.Click += (_, _) => LaunchFireworks();
        menu.Items.Add(fw);

        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "❌  Выход" };
        exit.Click += (_, _) => CloseApp();
        menu.Items.Add(exit);

        menu.IsOpen = true;
    }


    private void Settings_Click(object s, RoutedEventArgs e)
    {
        var sw = new SettingsWindow(_cfg);
        sw.Owner = this;
        if (sw.ShowDialog() == true)
        {
            ConfigService.Save(_cfg);
            _vm.ResetFireworks();
            ApplyTheme();
            ApplyOpacity();
            Topmost = _cfg.AlwaysOnTop;
        }
    }

    private void Close_Click(object s, RoutedEventArgs e) => CloseApp();

    private void CloseApp()
    {
        _cfg.PosX = Left; _cfg.PosY = Top;
        ConfigService.Save(_cfg);
        _timer.Stop();
        // Исправлено: System.Windows.Application.Current вместо неоднозначного Application.Current
        // (было: Application.Current.Shutdown();)
        System.Windows.Application.Current.Shutdown();
    }


    private void SwitchToMinimal()
    {
        _cfg.Minimalist = true;
        ConfigService.Save(_cfg);
        _timer.Stop();

        var mw = new MinimalWindow(_cfg);
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
}