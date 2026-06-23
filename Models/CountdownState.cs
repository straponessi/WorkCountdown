namespace WorkCountdown.Models;

/// <summary>
/// Состояние отсчёта рабочего дня
/// </summary>
public class CountdownState
{
    public WorkStatus Status { get; set; }
    public int Remaining { get; set; }       
    public double Progress { get; set; }    
    public DateTime EndTime { get; set; }   
}
