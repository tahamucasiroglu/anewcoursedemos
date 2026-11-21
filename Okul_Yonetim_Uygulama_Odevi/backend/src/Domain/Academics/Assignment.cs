namespace OkulYonetim.Domain.Academics;

/// <summary>
/// Ödev (spec 0013 C-1, gereksinim 1): kurum, şube, sınıf, veren öğretmen, ders adı (serbest, en fazla <see cref="SubjectMax"/>),
/// başlık (en fazla <see cref="TitleMax"/>), isteğe bağlı açıklama (en fazla <see cref="DescriptionMax"/>), teslim günü (Türkiye
/// günü) ve veriliş zamanı. Sınıfın tamamına verilir; değiştirme ve silme yöntemi yoktur (yanlışsa yeni ödev). Öğrencileri
/// verildiği andaki sınıf öğrencileridir (<see cref="AssignmentSubmission"/> satırları). Teslim günü bugün ile bugünden
/// <see cref="DueMaxDays"/> gün sonrası arasındadır (<see cref="IsDueDateAllowed"/>).
/// </summary>
public sealed class Assignment
{
    public const int SubjectMax = 60;
    public const int TitleMax = 150;
    public const int DescriptionMax = 2000;
    public const int DueMaxDays = 90;

    private Assignment(Guid id, Guid tenantId, Guid branchId, Guid classroomId, Guid createdByUserId, string subject, string title, string? description, DateOnly dueDate, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.ClassroomId = classroomId;
        this.CreatedByUserId = createdByUserId;
        this.Subject = subject;
        this.Title = title;
        this.Description = description;
        this.DueDate = dueDate;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid ClassroomId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string Subject { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public DateOnly DueDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Teslim günü bugün ile bugünden 90 gün sonrası arasında mı (uçlar dahil).</summary>
    public static bool IsDueDateAllowed(DateOnly dueDate, DateOnly today) =>
        dueDate >= today && dueDate <= today.AddDays(DueMaxDays);

    public static Assignment Create(Guid tenantId, Guid branchId, Guid classroomId, Guid createdByUserId, string subject, string title, string? description, DateOnly dueDate, DateOnly today, DateTimeOffset now)
    {
        string cleanSubject = Required(subject, SubjectMax, nameof(subject));
        string cleanTitle = Required(title, TitleMax, nameof(title));
        string? cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (cleanDescription is { Length: > DescriptionMax })
        {
            throw new ArgumentOutOfRangeException(nameof(description), "Açıklama en fazla 2000 karakter olabilir.");
        }

        if (!IsDueDateAllowed(dueDate, today))
        {
            throw new ArgumentOutOfRangeException(nameof(dueDate), "Teslim günü bugün ile 90 gün sonrası arasında olmalı.");
        }

        return new(Guid.CreateVersion7(now), tenantId, branchId, classroomId, createdByUserId, cleanSubject, cleanTitle, cleanDescription, dueDate, now);
    }

    private static string Required(string? value, int max, string name)
    {
        string clean = value?.Trim() ?? string.Empty;
        if (clean.Length == 0 || clean.Length > max)
        {
            throw new ArgumentOutOfRangeException(name, "Alan boş olamaz ve sınırı aşamaz.");
        }

        return clean;
    }
}
