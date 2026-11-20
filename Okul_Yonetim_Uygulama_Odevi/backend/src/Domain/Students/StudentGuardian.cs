namespace OkulYonetim.Domain.Students;

/// <summary>
/// Veli-öğrenci bağı (spec 0009): bir kişi (veli rolü olan kullanıcı) ile bir öğrenci arasındaki velilik ilişkisi ve
/// yakınlık. Aynı kişi aynı öğrenciye bir kez bağlanır; bir öğrencinin en fazla <see cref="MaxPerStudent"/> velisi olur.
/// Aynı veliye bağlı öğrenciler kardeştir (ayrı kayıt yok, C-4). Bağ kaldırılınca kişinin hesabı ve rolleri değişmez.
/// </summary>
public sealed class StudentGuardian
{
    public const int MaxPerStudent = 4;

    private StudentGuardian(Guid id, Guid tenantId, Guid studentId, Guid guardianUserId, GuardianRelationship relationship, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.StudentId = studentId;
        this.GuardianUserId = guardianUserId;
        this.Relationship = relationship;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public GuardianRelationship Relationship { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static StudentGuardian Link(Guid tenantId, Guid studentId, Guid guardianUserId, GuardianRelationship relationship, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, studentId, guardianUserId, relationship, now);
}
