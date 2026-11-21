using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Randevu deposu (spec 0016). Okumalar kurum filtresinden geçer; öğretmen, veli ve çocuk ilişkisi <c>AppointmentsGuard</c>'dadır.
/// Sıra içindeki taze okumalar (<see cref="HasActiveOnSlotAsync"/>, <see cref="HasOpenAsync"/>) öğretmen sırası
/// (<c>appointments:teacher:&lt;id&gt;</c>) içinde çağrılır; dilimde tek açık randevu kuralının ikinci katmanı kısmi tekil dizindir.
/// Liste satırları ad projeksiyonuyla ve izlenmeden okunur (T7).
/// </summary>
public interface IAppointmentRepository
{
    Task<Appointment?> FindAsync(Guid id, bool track, CancellationToken cancellationToken);

    /// <summary>Dilimde onay bekleyen ya da onaylı randevu var mı.</summary>
    Task<bool> HasActiveOnSlotAsync(Guid slotId, CancellationToken cancellationToken);

    /// <summary>Çocuk + öğretmen için açık (onay bekleyen ya da onaylı ve başlamamış) randevu var mı.</summary>
    Task<bool> HasOpenAsync(Guid studentId, Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Öğretmenin başlangıcı verilen andan sonra ya da o an olan, onay bekleyen ya da onaylı randevuları.</summary>
    Task<IReadOnlyList<AppointmentListing>> ListActiveForTeacherAsync(Guid teacherUserId, DateTimeOffset from, CancellationToken cancellationToken);

    /// <summary>Verilen çocukların ya da veliye ait (talep eden) randevular; başlangıcı verilen andan sonra olanlar.</summary>
    Task<IReadOnlyList<AppointmentListing>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, DateTimeOffset after, CancellationToken cancellationToken);

    /// <summary>Öğretmenin yanıt bekleyen (onay bekleyen ve başlamamış) talep sayısı.</summary>
    Task<int> CountPendingAsync(Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Kişinin ad-soyadı (bildirim metni için; loglanmaz); yoksa boş.</summary>
    Task<string> PersonNameAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Appointment appointment);
}
