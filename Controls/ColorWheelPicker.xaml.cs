using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorkCountdown.Controls;

/// <summary>
/// HSV colour picker: hue ring (WriteableBitmap) + SV square (gradient overlays).
/// </summary>
public partial class ColorWheelPicker : UserControl
{

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(ColorWheelPicker),
            new PropertyMetadata("Цвет"));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set { SetValue(LabelProperty, value); TitleLabel.Text = $"Выбор цвета: {value}"; }
    }

    /// <summary>Событие: пользователь нажал «Применить».</summary>
    public event EventHandler<Color>? ColorApplied;
    /// <summary>Событие: пользователь нажал «Отмена».</summary>
    public event EventHandler? Cancelled;


    private double _hue = 0.6;          // 0..1
    private double _sat = 0.6;          // 0..1
    private double _val = 1.0;          // 0..1

    private const int WhlSize = 200;
    private const int WhlOuter = 95;
    private const int WhlInner = 65;

    private bool _draggingWheel;
    private bool _draggingSv;
    private bool _suppressHex;

    public ColorWheelPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => { DrawWheel(); RenderAll(); };
    }

    /// <summary>Установить начальный цвет.</summary>
    public void SetColor(Color c)
    {
        RgbToHsv(c.R / 255.0, c.G / 255.0, c.B / 255.0,
                 out _hue, out _sat, out _val);
        RenderAll();
    }


    private void DrawWheel()
    {
        int sz = WhlSize;
        var bmp = new WriteableBitmap(sz, sz, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[sz * sz * 4];

        double cx = sz / 2.0, cy = sz / 2.0;

        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                double dx = x - cx, dy = y - cy;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < WhlInner || dist > WhlOuter) continue;

                // Угол: верх = hue 0, по часовой стрелке
                // atan2(dy, dx) в экранных координатах (y↓):
                // hue = (degrees + 90) % 360 / 360
                double deg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                double h = ((deg + 90.0) % 360.0 + 360.0) % 360.0 / 360.0;

                HsvToRgb(h, 1.0, 1.0, out double r, out double g, out double b);
                int idx = (y * sz + x) * 4;
                pixels[idx + 0] = (byte)(b * 255);
                pixels[idx + 1] = (byte)(g * 255);
                pixels[idx + 2] = (byte)(r * 255);
                pixels[idx + 3] = 255;
            }

        bmp.WritePixels(new Int32Rect(0, 0, sz, sz), pixels, sz * 4, 0);
        WheelImage.Source = bmp;
    }


    private void RenderAll()
    {
        UpdateHueIndicator();
        UpdateSvRect();
        UpdateSvIndicator();
        UpdatePreview();
    }

    private void UpdateHueIndicator()
    {
        double cx = WhlSize / 2.0, cy = WhlSize / 2.0;
        double mid = (WhlInner + WhlOuter) / 2.0;

        // hue=0 → верх (-90° в atan2), индикатор: angle = hue*360 - 90
        double ang = (_hue * 360.0 - 90.0) * Math.PI / 180.0;
        double mx = cx + mid * Math.Cos(ang) - 8;
        double my = cy + mid * Math.Sin(ang) - 8;

        HueIndicator.Margin = new Thickness(mx, my, 0, 0);
    }

    private void UpdateSvRect()
    {
        // Правый стоп градиента = чистый тон
        HsvToRgb(_hue, 1.0, 1.0, out double r, out double g, out double b);
        HueGradientStop.Color = Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private void UpdateSvIndicator()
    {
        const double sz = 160;
        double px = _sat * sz - 7;
        double py = (1.0 - _val) * sz - 7;
        SvIndicator.Margin = new Thickness(
            Math.Clamp(px, 0, sz - 14),
            Math.Clamp(py, 0, sz - 14), 0, 0);
    }

    private void UpdatePreview()
    {
        HsvToRgb(_hue, _sat, _val, out double r, out double g, out double b);
        var c = Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        PreviewBrush.Color = c;
        RgbLabel.Text = $"R {c.R}   G {c.G}   B {c.B}";

        _suppressHex = true;
        HexBox.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        _suppressHex = false;
    }

    private Color CurrentColor()
    {
        HsvToRgb(_hue, _sat, _val, out double r, out double g, out double b);
        return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }


    private void Wheel_MouseDown(object s, MouseButtonEventArgs e)
    {
        _draggingWheel = true; WheelImage.CaptureMouse(); ApplyWheelHit(e.GetPosition(WheelImage));
    }
    private void Wheel_MouseMove(object s, MouseEventArgs e)
    {
        if (_draggingWheel) ApplyWheelHit(e.GetPosition(WheelImage));
    }
    private void Wheel_MouseUp(object s, MouseButtonEventArgs e)
    {
        _draggingWheel = false; WheelImage.ReleaseMouseCapture();
    }

    private void ApplyWheelHit(Point p)
    {
        double cx = WhlSize / 2.0, cy = WhlSize / 2.0;
        double dx = p.X - cx, dy = p.Y - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < WhlInner - 12 || dist > WhlOuter + 12) return;

        double deg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        _hue = ((deg + 90.0) % 360.0 + 360.0) % 360.0 / 360.0;
        RenderAll();
    }


    private void SV_MouseDown(object s, MouseButtonEventArgs e)
    {
        _draggingSv = true;
        ((UIElement)s).CaptureMouse();
        ApplySvHit(e.GetPosition((IInputElement)s));
    }
    private void SV_MouseMove(object s, MouseEventArgs e)
    {
        if (_draggingSv) ApplySvHit(e.GetPosition((IInputElement)s));
    }
    private void SV_MouseUp(object s, MouseButtonEventArgs e)
    {
        _draggingSv = false; ((UIElement)s).ReleaseMouseCapture();
    }

    private void ApplySvHit(Point p)
    {
        const double sz = 160;
        _sat = Math.Clamp(p.X / sz, 0, 1);
        _val = Math.Clamp(1.0 - p.Y / sz, 0, 1);
        UpdateSvIndicator(); UpdatePreview();
    }


    private void HexBox_TextChanged(object s, TextChangedEventArgs e)
    {
        if (_suppressHex) return;
        TryParseHex(HexBox.Text);
    }

    private void HexBox_LostFocus(object s, RoutedEventArgs e)
        => TryParseHex(HexBox.Text);

    private void TryParseHex(string text)
    {
        var raw = text.TrimStart('#');
        if (raw.Length != 6) return;
        try
        {
            byte r = Convert.ToByte(raw[0..2], 16);
            byte g = Convert.ToByte(raw[2..4], 16);
            byte b = Convert.ToByte(raw[4..6], 16);
            RgbToHsv(r / 255.0, g / 255.0, b / 255.0, out _hue, out _sat, out _val);
            RenderAll();
        }
        catch { }
    }


    private void Apply_Click(object s, RoutedEventArgs e) => ColorApplied?.Invoke(this, CurrentColor());
    private void Cancel_Click(object s, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);


    public static void HsvToRgb(double h, double s, double v,
                                 out double r, out double g, out double b)
    {
        if (s < 1e-9) { r = g = b = v; return; }
        double hh = h * 6.0;
        int i = (int)hh % 6;
        double f = hh - Math.Floor(hh);
        double p = v * (1 - s);
        double q = v * (1 - s * f);
        double t = v * (1 - s * (1 - f));
        (r, g, b) = i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };
    }

    public static void RgbToHsv(double r, double g, double b,
                                  out double h, out double s, out double v)
    {
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        v = max;
        s = max < 1e-9 ? 0 : delta / max;
        if (delta < 1e-9) { h = 0; return; }
        if (max == r) h = (g - b) / delta / 6.0;
        else if (max == g) h = ((b - r) / delta + 2.0) / 6.0;
        else h = ((r - g) / delta + 4.0) / 6.0;
        if (h < 0) h += 1;
    }

    public static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    public static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}