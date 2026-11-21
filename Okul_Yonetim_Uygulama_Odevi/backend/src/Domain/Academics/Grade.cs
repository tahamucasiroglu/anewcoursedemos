namespace OkulYonetim.Domain.Academics;

/// <summary>
/// Bir değerlendirmede bir öğrencinin puanı (spec 0013 C-3, gereksinim 6): değerlendirme + öğrenci tekil; puan
/// <see cref="MinScore"/>–<see cref="MaxScore"/> arası tam sayı ya da boş ("girilmedi"). Sonradan düzeltilir ya da silinir
/// (boş); değiştiren ve zaman güncellenir.
/// </summary>
public sealed class Grade
{
    public const int MinScore = 0;
    public const int MaxScore = 100;

    private Grade(Guid id, Guid tenantId, Guid assessmentId, Guid studentId, Guid updatedByUserId, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AssessmentId = assessmentId;
        this.StudentId = studentId;
        this.UpdatedByUserId = updatedByUserId;
        this.UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AssessmentId { get; private set; }

    public Guid StudentId { get; private set; }

    public int? Score { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static bool IsValidScore(int? score) => score is null or (>= MinScore and <= MaxScore);

    /// <summary>Değerlendirme açılınca öğrencinin boş ("girilmedi") puan satırı.</summary>
    public static Grade Start(Assessment assessment, Guid studentId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        return new(Guid.CreateVersion7(now), assessment.TenantId, assessment.Id, studentId, assessment.CreatedByUserId, now);
    }

    /// <summary>Puanı yazar, düzeltir ya da siler (<c>null</c>); gerçekten değiştiyse <c>true</c>. Geçersiz puan reddedilir.</summary>
    public bool SetScore(int? score, Guid updatedByUserId, DateTimeOffset now)
    {
        if (!IsValidScore(score))
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Puan 0 ile 100 arasında tam sayı olmalı.");
        }

        if (score == this.Score)
        {
            return false;
        }

        this.Score = score;
        this.UpdatedByUserId = updatedByUserId;
        this.UpdatedAt = now;
        return true;
    }
}
