namespace WorkCountdown.Models;

/// <summary>
/// Состояние отсчёта рабочего дня
/// </summary>
public class CountdownState
{
    public WorkStatus Status { get; set; }
    public int Remaining { get; set; }       // Оставшиеся секунды
    public double Progress { get; set; }     // 0.0 до 1.0
    public DateTime EndTime { get; set; }    // Время окончания рабочего дня
}
