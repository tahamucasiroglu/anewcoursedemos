using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Görüşme dilimleri deposu (spec 0016). Okumalar kurum filtresinden geçer ve kaldırılmış dilimleri göstermez; öğretmen ve ilişki
/// duvarı <c>AppointmentsGuard</c>'dadır. Listeler izlenmez (T7); değiştirilecek dilim <c>track</c> ile okunur.
/// </summary>
public interface IAvailabilitySlotRepository
{
    Task<AvailabilitySlot?> FindAsync(Guid id, bool track, CancellationToken cancellationToken);

    /// <summary>Öğretmenin başlangıcı verilen andan sonra olan (ya da <paramref name="inclusive"/> ise o an olan) dilimleri, saat sırasıyla.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> ListForTeacherAsync(Guid teacherUserId, DateTimeOffset from, bool inclusive, CancellationToken cancellationToken);

    /// <summary>Öğretmenin başlamamış ve boş (onay bekleyen ya da onaylı randevusu olmayan) dilimleri, saat sırasıyla.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> ListFreeForTeacherAsync(Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(AvailabilitySlot slot);
}
