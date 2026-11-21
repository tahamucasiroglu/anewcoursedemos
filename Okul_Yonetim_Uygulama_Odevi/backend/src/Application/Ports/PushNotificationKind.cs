namespace OkulYonetim.Application.Ports;

/// <summary>Bildirimin türü (spec 0011): duyuru, onay formu, mesaj ya da diğer modüllerin okul bildirimi (yoklama, spec 0010); spec 0016: öğretmene giden randevu bildirimi (yeni talep, veli iptali).</summary>
public enum PushNotificationKind
{
    Announcement = 1,
    ConsentForm = 2,
    Message = 3,
    SchoolNotice = 4,
    Appointment = 5,
}
