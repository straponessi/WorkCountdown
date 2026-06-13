using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WorkCountdown.Windows;

public partial class FireworksWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly Random _rng = new();
    private readonly List<Particle> _particles = new();

    private const double Gravity = 0.22;
    private const double Duration = 9.0;   // секунд
    private readonly DateTime _start = DateTime.Now;

    private static readonly string[] Colors =
    [
        "#FF6B6B","#FFD93D","#6BCB77","#4D96FF",
        "#FF6BD6","#C7F464","#FF9A3C","#A29BFE",
        "#F72585","#7209B7","#3A86FF","#80FFDB",
    ];

    public FireworksWindow()
    {
        InitializeComponent();

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 30),
        };
        _timer.Tick += Frame;
        _timer.Start();

        // Первый залп
        SpawnBurst();
    }


    private void Frame(object? s, EventArgs e)
    {
        if ((DateTime.Now - _start).TotalSeconds > Duration)
        {
            _timer.Stop();
            Close();
            return;
        }

        if (_rng.NextDouble() < 0.22) SpawnBurst();

        ParticleCanvas.Children.Clear();

        var dead = new List<Particle>();
        foreach (var p in _particles)
        {
            p.X += p.Vx;
            p.Y += p.Vy;
            p.Vy += Gravity;
            p.Life -= p.Decay;
            if (p.Life <= 0) { dead.Add(p); continue; }

            byte alpha = (byte)(255 * p.Life);
            byte r = ScaleChannel(p.BaseColor.R, p.Life);
            byte g = ScaleChannel(p.BaseColor.G, p.Life);
            byte b = ScaleChannel(p.BaseColor.B, p.Life);
            var col = Color.FromArgb(alpha, r, g, b);

            double sz = p.Size * p.Life;
            var ell = new Ellipse
            {
                Width = sz,
                Height = sz,
                Fill = new SolidColorBrush(col),
            };
            Canvas.SetLeft(ell, p.X - sz / 2);
            Canvas.SetTop(ell, p.Y - sz / 2);
            ParticleCanvas.Children.Add(ell);
        }

        foreach (var d in dead) _particles.Remove(d);
    }


    private void SpawnBurst()
    {
        double w = ActualWidth > 0 ? ActualWidth : 1920;
        double h = ActualHeight > 0 ? ActualHeight : 1080;

        double cx = _rng.NextDouble() * (w - 160) + 80;
        double cy = _rng.NextDouble() * (h / 2 - 40) + 50;

        var hex = Colors[_rng.Next(Colors.Length)].TrimStart('#');
        var col = Color.FromRgb(
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));

        int n = _rng.Next(60, 90);
        for (int i = 0; i < n; i++)
        {
            double angle = _rng.NextDouble() * Math.PI * 2;
            double speed = _rng.NextDouble() * 6 + 3;
            _particles.Add(new Particle
            {
                X = cx,
                Y = cy,
                Vx = Math.Cos(angle) * speed,
                Vy = Math.Sin(angle) * speed,
                Life = 1.0,
                Decay = _rng.NextDouble() * 0.018 + 0.010,
                Size = _rng.NextDouble() * 5 + 2,
                BaseColor = col,
            });
        }
    }

    private void Canvas_Click(object s, MouseButtonEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }

    private static byte ScaleChannel(byte ch, double life)
        => (byte)Math.Clamp(ch * life, 0, 255);


    private class Particle
    {
        public double X, Y, Vx, Vy;
        public double Life, Decay, Size;
        public Color BaseColor;
    }
}