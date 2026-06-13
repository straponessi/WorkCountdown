using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorkCountdown.Controls;
using WorkCountdown.Models;
using WorkCountdown.Services;

namespace WorkCountdown.Windows;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;

    private string? _colorBg;
    private string? _colorTimer;

    public SettingsWindow(AppConfig cfg)
    {
        _cfg = cfg;
        _colorBg = cfg.ColorBg;
        _colorTimer = cfg.ColorTimer;

        InitializeComponent();

        LoadValues();
    }


    private void LoadValues()
    {
        TbStart.Text = _cfg.WorkStart;
        TbEndWd.Text = _cfg.WorkEndWeekday;
        TbEndFr.Text = _cfg.WorkEndFriday;

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_cfg.CustomDate == today)
        {
            TbCustStart.Text = _cfg.CustomStart ?? "";
            TbCustEnd.Text = _cfg.CustomEnd ?? "";
        }

        // Тема
        CbTheme.SelectedIndex = _cfg.Theme == "light" ? 1 : 0;

        // Прозрачность
        OpacitySlider.Value = _cfg.Opacity;
        OpacityLabel.Text = $"{(int)(_cfg.Opacity * 100)}%";

        // Цвета
        RefreshBgSwatch();
        RefreshTimerSwatch();

        // Чекбоксы
        ChkAlwaysOnTop.IsChecked = _cfg.AlwaysOnTop;
        ChkMinimalist.IsChecked = _cfg.Minimalist;
        ChkMiniPct.IsChecked = _cfg.MiniShowPct;
        ChkAutostart.IsChecked = AutostartService.IsEnabled();
    }

    private void RefreshBgSwatch()
    {
        var hex = _colorBg ?? ThemeDefaults.Bg(_cfg.Theme);
        BgSwatch.Background = BrushFromHex(hex);
        BgHexLabel.Text = hex.ToUpperInvariant();
    }

    private void RefreshTimerSwatch()
    {
        var hex = _colorTimer ?? ThemeDefaults.Accent(_cfg.Theme);
        TimerSwatch.Background = BrushFromHex(hex);
        TimerHexLabel.Text = hex.ToUpperInvariant();
    }


    private void Header_DragMove(object s, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }


    private void TimeBox_GotFocus(object s, RoutedEventArgs e)
    {
        // Исправлено: System.Windows.Controls.TextBox вместо неоднозначного TextBox
        // (было: if (s is TextBox tb) tb.SelectAll();)
        if (s is System.Windows.Controls.TextBox tb) tb.SelectAll();
    }

    private void TimeBox_PreviewInput(object s, TextCompositionEventArgs e)
    {
        // Разрешаем только цифры
        e.Handled = !char.IsDigit(e.Text, 0);
    }

    private bool _maskBusy;

    private void TimeBox_TextChanged(object s, TextChangedEventArgs e)
    {
        // Исправлено: System.Windows.Controls.TextBox вместо неоднозначного TextBox
        // (было: if (_maskBusy || s is not TextBox tb) return;)
        if (_maskBusy || s is not System.Windows.Controls.TextBox tb) return;
        _maskBusy = true;
        try
        {
            int caret = tb.CaretIndex;
            string raw = new string(tb.Text.Where(char.IsDigit).ToArray());
            if (raw.Length > 4) raw = raw[..4];

            string result = raw.Length >= 3
                ? $"{raw[..2]}:{raw[2..]}"
                : raw.Length == 2 ? $"{raw}:" : raw;

            if (result != tb.Text)
            {
                tb.Text = result;
                tb.CaretIndex = Math.Min(result.Length, caret + 1);
            }
        }
        finally { _maskBusy = false; }
    }


    private void CbTheme_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        // Обновляем свотчи при смене темы, если цвет не кастомный
        if (_colorBg == null) RefreshBgSwatch();
        if (_colorTimer == null) RefreshTimerSwatch();
    }

    private void Opacity_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel == null) return;
        OpacityLabel.Text = $"{(int)(OpacitySlider.Value * 100)}%";
    }


    private void BgSwatch_Click(object s, MouseButtonEventArgs e)
        => OpenPicker("Цвет фона", _colorBg ?? ThemeDefaults.Bg(_cfg.Theme),
            hex => { _colorBg = hex; RefreshBgSwatch(); });

    private void TimerSwatch_Click(object s, MouseButtonEventArgs e)
        => OpenPicker("Цвет таймера", _colorTimer ?? ThemeDefaults.Accent(_cfg.Theme),
            hex => { _colorTimer = hex; RefreshTimerSwatch(); });

    private void BgReset_Click(object s, RoutedEventArgs e)
    {
        _colorBg = null;
        RefreshBgSwatch();
    }

    private void TimerReset_Click(object s, RoutedEventArgs e)
    {
        _colorTimer = null;
        RefreshTimerSwatch();
    }

    private void OpenPicker(string label, string initial, Action<string> onApply)
    {
        var dlg = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Transparent,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ShowInTaskbar = false,
        };

        var picker = new ColorWheelPicker { Label = label };
        picker.SetColor(ColorWheelPicker.HexToColor(initial));

        picker.ColorApplied += (_, c) =>
        {
            onApply(ColorWheelPicker.ColorToHex(c));
            dlg.Close();
        };
        picker.Cancelled += (_, _) => dlg.Close();

        dlg.Content = picker;
        dlg.ShowDialog();
    }


    private bool ValidateAll()
    {
        var required = new[] { (TbStart, "Начало"), (TbEndWd, "Конец Пн–Чт"), (TbEndFr, "Конец Пт") };
        foreach (var (tb, name) in required)
        {
            if (!CountdownService.TryParseTime(tb.Text, out _))
            {
                // Исправлено: System.Windows.MessageBox вместо неоднозначного MessageBox
                // (было: MessageBox.Show($"Неверный формат «{name}»: {tb.Text}\nФормат: ЧЧ:ММ", ...))
                System.Windows.MessageBox.Show($"Неверный формат «{name}»: {tb.Text}\nФормат: ЧЧ:ММ",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                tb.Focus(); return false;
            }
        }

        bool hasCs = !string.IsNullOrWhiteSpace(TbCustStart.Text);
        bool hasCe = !string.IsNullOrWhiteSpace(TbCustEnd.Text);
        if (hasCs || hasCe)
        {
            if (!CountdownService.TryParseTime(TbCustStart.Text, out _)
             || !CountdownService.TryParseTime(TbCustEnd.Text, out _))
            {
                // Исправлено: System.Windows.MessageBox вместо неоднозначного MessageBox
                // (было: MessageBox.Show("Неверный формат кастомного времени.\nФормат: ЧЧ:ММ", ...))
                System.Windows.MessageBox.Show("Неверный формат кастомного времени.\nФормат: ЧЧ:ММ",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        return true;
    }


    private void Save_Click(object s, RoutedEventArgs e)
    {
        if (!ValidateAll()) return;

        _cfg.WorkStart = TbStart.Text.Trim();
        _cfg.WorkEndWeekday = TbEndWd.Text.Trim();
        _cfg.WorkEndFriday = TbEndFr.Text.Trim();

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        bool hasCustom = !string.IsNullOrWhiteSpace(TbCustStart.Text)
                      && !string.IsNullOrWhiteSpace(TbCustEnd.Text);
        if (hasCustom)
        {
            _cfg.CustomStart = TbCustStart.Text.Trim();
            _cfg.CustomEnd = TbCustEnd.Text.Trim();
            _cfg.CustomDate = today;
        }
        else
        {
            _cfg.CustomStart = _cfg.CustomEnd = _cfg.CustomDate = null;
        }

        _cfg.Theme = ((ComboBoxItem)CbTheme.SelectedItem).Tag as string ?? "dark";
        _cfg.Opacity = Math.Round(OpacitySlider.Value, 2);
        _cfg.ColorBg = _colorBg;
        _cfg.ColorTimer = _colorTimer;
        _cfg.AlwaysOnTop = ChkAlwaysOnTop.IsChecked ?? false;
        _cfg.Minimalist = ChkMinimalist.IsChecked ?? false;
        _cfg.MiniShowPct = ChkMiniPct.IsChecked ?? true;

        AutostartService.Set(ChkAutostart.IsChecked ?? false);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }


    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            // Исправлено: System.Windows.Media.Color.FromRgb вместо неоднозначного Color.FromRgb
            // (было: return new SolidColorBrush(Color.FromRgb(...)))
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16)));
        }
        catch { return new SolidColorBrush(Colors.Gray); }
    }

    // Исправлено: System.Windows.Media.Brush вместо неоднозначного Brush
    // (было: private static readonly Brush Transparent = new SolidColorBrush(Colors.Transparent);)
    private static readonly System.Windows.Media.Brush Transparent = new SolidColorBrush(Colors.Transparent);
}