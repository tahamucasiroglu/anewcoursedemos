namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Veliye giden uygulama içi bildirim (spec 0010 gereksinim 6–8): kurum, veli (kişi), öğrenci, gün ve tür. Veli + öğrenci +
/// gün tekildir — aynı gün ikinci "yok" bildirimi oluşmaz (C-5). "Yok" düzeltilince aynı satır <see cref="GuardianNoticeKind.Corrected"/>
/// olur ("düzeltildi" etiketi, düzeltme metni) ve yeniden okunmamış sayılır; yeniden "Yok" olursa tekrar devamsızlık
/// bildirimi olur. Metin saklanmaz: ekranda tür, öğrenci adı, gün ve okul adından kurulur (gizlilik, gereksinim 10).
/// </summary>
public sealed class GuardianNotice
{
    private GuardianNotice(Guid id, Guid tenantId, Guid guardianUserId, Guid studentId, DateOnly day, GuardianNoticeKind kind, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.GuardianUserId = guardianUserId;
        this.StudentId = studentId;
        this.Day = day;
        this.Kind = kind;
        this.CreatedAt = createdAt;
        this.UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateOnly Day { get; private set; }

    public GuardianNoticeKind Kind { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static GuardianNotice Absent(Guid tenantId, Guid guardianUserId, Guid studentId, DateOnly day, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, guardianUserId, studentId, day, GuardianNoticeKind.Absent, now, now);

    /// <summary>Türü değiştirir ve bildirimi yeniden okunmamış yapar (düzeltme ya da yeniden devamsızlık).</summary>
    public void Change(GuardianNoticeKind kind, DateTimeOffset now)
    {
        this.Kind = kind;
        this.ReadAt = null;
        this.UpdatedAt = now;
    }

    public void MarkRead(DateTimeOffset now) => this.ReadAt ??= now;
}
