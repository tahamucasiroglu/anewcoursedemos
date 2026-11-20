namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Onay formunda bir öğrencinin cevabı (spec 0011 C-4): duyuru + öğrenci başına tek satır. Öğrencinin herhangi bir velisi
/// verir ve son güne kadar değiştirir; son cevap geçerlidir, kimin ve ne zaman verdiği tutulur. Süre dolduysa
/// <see cref="Give"/> ve <see cref="Change"/> <see cref="InvalidOperationException"/> fırlatır (çağıran önce
/// <see cref="Announcement.AcceptsConsentOn"/> ile denetler ve kullanıcıya "Onay süresi doldu." der).
/// </summary>
public sealed class ConsentResponse
{
    private ConsentResponse(Guid id, Guid tenantId, Guid announcementId, Guid studentId, ConsentAnswer answer, Guid answeredByUserId, DateTimeOffset answeredAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AnnouncementId = announcementId;
        this.StudentId = studentId;
        this.Answer = answer;
        this.AnsweredByUserId = answeredByUserId;
        this.AnsweredAt = answeredAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AnnouncementId { get; private set; }

    public Guid StudentId { get; private set; }

    public ConsentAnswer Answer { get; private set; }

    public Guid AnsweredByUserId { get; private set; }

    public DateTimeOffset AnsweredAt { get; private set; }

    public static ConsentResponse Give(Announcement announcement, Guid studentId, ConsentAnswer answer, Guid guardianUserId, DateTimeOffset now, DateOnly turkeyDay)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        EnsureOpen(announcement, turkeyDay);
        return new(Guid.CreateVersion7(now), announcement.TenantId, announcement.Id, studentId, answer, guardianUserId, now);
    }

    public void Change(Announcement announcement, ConsentAnswer answer, Guid guardianUserId, DateTimeOffset now, DateOnly turkeyDay)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        EnsureOpen(announcement, turkeyDay);
        this.Answer = answer;
        this.AnsweredByUserId = guardianUserId;
        this.AnsweredAt = now;
    }

    private static void EnsureOpen(Announcement announcement, DateOnly turkeyDay)
    {
        if (!announcement.AcceptsConsentOn(turkeyDay))
        {
            throw new InvalidOperationException("Onay formu kapalı ya da süresi doldu.");
        }
    }
}
