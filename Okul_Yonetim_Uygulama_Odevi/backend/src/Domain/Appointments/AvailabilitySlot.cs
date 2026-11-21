namespace OkulYonetim.Domain.Appointments;

/// <summary>
/// Öğretmenin bir görüşme dilimi (spec 0016 C-1): okul, öğretmen (kişi), başlangıç ve bitiş (UTC), oluşturulma ve kaldırılma
/// zamanı. Müsaitlik okul başınadır. Kaldırılan dilim hiçbir listede görünmez (veritabanı filtresi); öğretmen boş dilimi
/// kaldırınca ya da onaylı randevuyu iptal edince dilim kapanır (C-4). "Talebi olan dilim kaldırılamaz" kuralı işleyicide,
/// öğretmen sırası içinde denetlenir.
/// </summary>
public sealed class AvailabilitySlot
{
    private AvailabilitySlot(Guid id, Guid tenantId, Guid teacherUserId, SlotWindow window, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.TeacherUserId = teacherUserId;
        this.StartsAt = window.StartsAt;
        this.EndsAt = window.EndsAt;
        this.CreatedAt = createdAt;
    }

    private AvailabilitySlot()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public static AvailabilitySlot Open(Guid tenantId, Guid teacherUserId, SlotWindow window, DateTimeOffset now) =>
        window.EndsAt <= window.StartsAt || window.StartsAt <= now
            ? throw new ArgumentOutOfRangeException(nameof(window), "Dilim başlamamış ve bitişi başlangıcından sonra olmalı.")
            : new(Guid.CreateVersion7(now), tenantId, teacherUserId, window, now);

    /// <summary>Dilim başlamış mı (başlangıç şu an ya da önce).</summary>
    public bool HasStarted(DateTimeOffset now) => this.StartsAt <= now;

    /// <summary>Dilimi kapatır (boş dilimi kaldırma ya da öğretmen iptali).</summary>
    public void Remove(DateTimeOffset now) => this.RemovedAt ??= now;
}
