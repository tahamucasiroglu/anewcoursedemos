namespace OkulYonetim.Domain.Academics;

/// <summary>
/// Değerlendirme — yazılı ya da sözlü not (spec 0013 C-3, gereksinim 5): kurum, şube, sınıf, açan öğretmen, ders adı (en fazla
/// <see cref="SubjectMax"/>), tür, başlık (ör. "1. yazılı", en fazla <see cref="TitleMax"/>), gün ve açılış zamanı. Gün bugünden
/// sonra olamaz ve sınıfın dönemi içindedir (dönem denetimi uygulamada). Öğrencileri açıldığı andaki sınıf öğrencileridir
/// (<see cref="Grade"/> satırları). Ortalama, ağırlık ve harf notu yoktur.
/// </summary>
public sealed class Assessment
{
    public const int SubjectMax = 60;
    public const int TitleMax = 100;

    private Assessment(Guid id, Guid tenantId, Guid branchId, Guid classroomId, Guid createdByUserId, string subject, AssessmentKind kind, string title, DateOnly date, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.ClassroomId = classroomId;
        this.CreatedByUserId = createdByUserId;
        this.Subject = subject;
        this.Kind = kind;
        this.Title = title;
        this.Date = date;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid ClassroomId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string Subject { get; private set; }

    public AssessmentKind Kind { get; private set; }

    public string Title { get; private set; }

    public DateOnly Date { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Assessment Create(Guid tenantId, Guid branchId, Guid classroomId, Guid createdByUserId, string subject, AssessmentKind kind, string title, DateOnly date, DateOnly today, DateTimeOffset now)
    {
        string cleanSubject = subject?.Trim() ?? string.Empty;
        string cleanTitle = title?.Trim() ?? string.Empty;
        if (cleanSubject.Length == 0 || cleanSubject.Length > SubjectMax || cleanTitle.Length == 0 || cleanTitle.Length > TitleMax)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "Ders adı ve başlık boş olamaz ve sınırı aşamaz.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Tür yazılı ya da sözlü olmalı.");
        }

        if (date > today)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "Değerlendirme günü bugünden sonra olamaz.");
        }

        return new(Guid.CreateVersion7(now), tenantId, branchId, classroomId, createdByUserId, cleanSubject, kind, cleanTitle, date, now);
    }
}
