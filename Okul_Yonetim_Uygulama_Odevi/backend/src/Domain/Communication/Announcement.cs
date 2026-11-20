namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Duyuru (spec 0011 gereksinim 1, 4): okul, yayınlayan (kişi), kapsam (<see cref="AnnouncementScope"/> + şube/sınıf/öğrenci
/// kimliği; okulda boş), başlık, metin, yayın zamanı ve isteğe bağlı onay formu (soru + son gün, Türkiye günü). Yalnızca
/// <see cref="Publish"/> ile oluşur; yayınlandıktan sonra değiştirilemez ve silinemez (yanlışsa yeni duyuru). Onay cevabı son
/// gün dahil kabul edilir (<see cref="AcceptsConsentOn"/>).
/// </summary>
public sealed class Announcement
{
    public const int TitleMaxLength = 150;
    public const int BodyMaxLength = 4000;
    public const int QuestionMaxLength = 300;
    public const int MaxDeadlineDays = 180;

    private Announcement(Guid id, Guid tenantId, Guid authorUserId, AnnouncementScope scope, Guid? scopeId, string title, string body, string? consentQuestion, DateOnly? consentDeadline, DateTimeOffset publishedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AuthorUserId = authorUserId;
        this.Scope = scope;
        this.ScopeId = scopeId;
        this.Title = title;
        this.Body = body;
        this.ConsentQuestion = consentQuestion;
        this.ConsentDeadline = consentDeadline;
        this.PublishedAt = publishedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public AnnouncementScope Scope { get; private set; }

    public Guid? ScopeId { get; private set; }

    public string Title { get; private set; }

    public string Body { get; private set; }

    public string? ConsentQuestion { get; private set; }

    public DateOnly? ConsentDeadline { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }

    public bool RequiresConsent => this.ConsentQuestion is not null;

    public static Announcement Publish(Guid tenantId, Guid authorUserId, AnnouncementScope scope, Guid? scopeId, string title, string body, string? consentQuestion, DateOnly? consentDeadline, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);
        bool consent = !string.IsNullOrWhiteSpace(consentQuestion) && consentDeadline is not null;
        return new(
            Guid.CreateVersion7(now),
            tenantId,
            authorUserId,
            scope,
            scope == AnnouncementScope.School ? null : scopeId,
            title.Trim(),
            body.Trim(),
            consent ? consentQuestion!.Trim() : null,
            consent ? consentDeadline : null,
            now);
    }

    /// <summary>Verilen Türkiye gününde onay cevabı kabul edilir mi: onay formu var ve gün son günden sonra değil.</summary>
    public bool AcceptsConsentOn(DateOnly turkeyDay) => this.ConsentDeadline is { } deadline && turkeyDay <= deadline;
}
