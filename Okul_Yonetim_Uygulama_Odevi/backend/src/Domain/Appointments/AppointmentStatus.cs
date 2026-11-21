namespace OkulYonetim.Domain.Appointments;

/// <summary>
/// Randevunun saklanan durumu (spec 0016 C-3, C-4): onay bekliyor, onaylandı, reddedildi, veli iptal etti, öğretmen iptal etti.
/// "Süresi doldu" saklanmaz; onay bekleyen talebin dilimi başlayınca okumada hesaplanır (<see cref="Appointment.StateAt"/>).
/// </summary>
public enum AppointmentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    CancelledByGuardian = 4,
    CancelledByTeacher = 5,
}
