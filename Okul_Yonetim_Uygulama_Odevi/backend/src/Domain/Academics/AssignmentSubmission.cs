namespace OkulYonetim.Domain.Academics;

/// <summary>
/// Ödevde bir öğrencinin teslim durumu (spec 0013 C-2, gereksinim 4): ödev + öğrenci tekil; ödev verilince "İşaretlenmedi"
/// başlar, öğretmen değiştirdikçe durum, değiştiren ve zaman güncellenir (gün kilidi yok). Öğrenci sınıftan ayrılsa da satır
/// silinmez (velinin özetinde kalır).
/// </summary>
public sealed class AssignmentSubmission
{
    private AssignmentSubmission(Guid id, Guid tenantId, Guid assignmentId, Guid studentId, SubmissionStatus status, Guid updatedByUserId, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AssignmentId = assignmentId;
        this.StudentId = studentId;
        this.Status = status;
        this.UpdatedByUserId = updatedByUserId;
        this.UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid StudentId { get; private set; }

    public SubmissionStatus Status { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AssignmentSubmission Start(Assignment assignment, Guid studentId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return new(Guid.CreateVersion7(now), assignment.TenantId, assignment.Id, studentId, SubmissionStatus.Unmarked, assignment.CreatedByUserId, now);
    }

    /// <summary>Durumu değiştirir; gerçekten değiştiyse <c>true</c> (değişmeyen satır yazılmaz).</summary>
    public bool Set(SubmissionStatus status, Guid updatedByUserId, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Geçersiz teslim durumu.");
        }

        if (status == this.Status)
        {
            return false;
        }

        this.Status = status;
        this.UpdatedByUserId = updatedByUserId;
        this.UpdatedAt = now;
        return true;
    }
}
