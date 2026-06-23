using WorkCountdown.Infrastructure;
using WorkCountdown.Models;
using WorkCountdown.Services;

namespace WorkCountdown.Windows;

public class MainViewModel : ViewModelBase
{
    private readonly AppConfig _cfg;


    private string _timerText = "00:00:00";
    private string _dayLabel = "";
    private string _statusText = "до конца рабочего дня";
    private string _pctText = "—";
    private double _progress = 0;
    private System.Windows.Media.Color _timerColor;
    private System.Windows.Media.Color _barColor;
    private WorkStatus _status = WorkStatus.Before;
    private bool _fireworksTriggered;

    public string TimerText { get => _timerText; private set => Set(ref _timerText, value); }
    public string DayLabel { get => _dayLabel; private set => Set(ref _dayLabel, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string PctText { get => _pctText; private set => Set(ref _pctText, value); }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public System.Windows.Media.Color TimerColor { get => _timerColor; private set => Set(ref _timerColor, value); }
    public System.Windows.Media.Color BarColor { get => _barColor; private set => Set(ref _barColor, value); }

    public event EventHandler? WorkDayDone;


    private static readonly string[] DaysRu =
        ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

    private static readonly string[] DayMoods = ["😤", "😐", "😑", "🙂", "🎉", "😎", "😎"];

    public MainViewModel(AppConfig cfg)
    {
        _cfg = cfg;
        var t = cfg.EffectiveTimer;
        _timerColor = ParseColor(t);
        _barColor = ParseColor(t);
    }


    public void Tick()
    {
        var state = CountdownService.GetState(_cfg);
        var now = DateTime.Now;
        int wd = ((int)now.DayOfWeek + 6) % 7;   
        string day = $"{DayMoods[wd]} {DaysRu[wd]}";

        var accentColor = ParseColor(_cfg.EffectiveTimer);
        var warnColor = ParseColor(ThemeDefaults.Warn(_cfg.Theme));
        var okColor = ParseColor(ThemeDefaults.Ok(_cfg.Theme));

        switch (state.Status)
        {
            case WorkStatus.Before:
                DayLabel = $"{day}  •  с {_cfg.WorkStart}";
                TimerText = CountdownService.FormatHms(state.Remaining);
                StatusText = "до начала рабочего дня";
                PctText = "—";
                Progress = 0;
                TimerColor = ParseColor(ThemeDefaults.Dim(_cfg.Theme));
                break;

            case WorkStatus.Working:
                bool custom = _cfg.CustomDate == now.ToString("yyyy-MM-dd")
                              && !string.IsNullOrEmpty(_cfg.CustomStart);
                string tag = custom ? "  🔧" : "";
                DayLabel = $"{day}  •  до {state.EndTime:HH:mm}{tag}";
                TimerText = CountdownService.FormatHms(state.Remaining);
                StatusText = "осталось";
                PctText = $"{(int)(state.Progress * 100)}%";
                Progress = state.Progress;
                TimerColor = state.Remaining < 1800
                    ? (now.Second % 2 == 0 ? warnColor : okColor)
                    : accentColor;
                break;

            case WorkStatus.Done:
                DayLabel = $"{day}  •  СВОБОДЕН! 🍻";
                TimerText = "00:00:00";
                StatusText = "Рабочий день окончен!  🎉";
                PctText = "100%";
                Progress = 1.0;
                TimerColor = okColor;
                if (!_fireworksTriggered)
                {
                    _fireworksTriggered = true;
                    WorkDayDone?.Invoke(this, EventArgs.Empty);
                }
                break;
        }

        BarColor = TimerColor;
        _status = state.Status;
    }

    public void ResetFireworks() => _fireworksTriggered = false;

    private static System.Windows.Media.Color ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return System.Windows.Media.Color.FromRgb(0x7C, 0x6A, 0xFF);
        try
        {
            hex = hex.TrimStart('#');
            return System.Windows.Media.Color.FromRgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }
        catch { return System.Windows.Media.Color.FromRgb(0x7C, 0x6A, 0xFF); }
    }
}