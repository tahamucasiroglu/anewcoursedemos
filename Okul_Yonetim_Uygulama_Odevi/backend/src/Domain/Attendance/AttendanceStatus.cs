namespace OkulYonetim.Domain.Attendance;

/// <summary>Yoklamada öğrencinin durumu (spec 0010 C-2): Var, Yok, Geç. Yalnızca "Yok" veliye bildirim üretir.</summary>
public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
}
