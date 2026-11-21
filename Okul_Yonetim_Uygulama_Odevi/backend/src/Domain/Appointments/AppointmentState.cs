namespace OkulYonetim.Domain.Appointments;

/// <summary>
/// Randevunun ekranda görünen durumu (spec 0016 gereksinim 8): saklanan durum ve ondan hesaplanan "Süresi doldu" — dilimi
/// başlamış (başlangıç ≤ şimdi) ve hâlâ onay bekleyen talep.
/// </summary>
public enum AppointmentState
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    CancelledByGuardian = 4,
    CancelledByTeacher = 5,
    Expired = 6,
}
