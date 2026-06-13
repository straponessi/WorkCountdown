namespace WorkCountdown.Models;

/// <summary>
/// Статус рабочего времени
/// </summary>
public enum WorkStatus
{
    Before,   // До начала рабочего дня
    Working,  // Идёт рабочий день
    Done      // Рабочий день закончен
}
