namespace OkulYonetim.Domain.Students;

/// <summary>
/// Öğrenci kartı (spec 0009): kuruma ve bir şubeye bağlıdır; şubesi kayıttan sonra değişmez (nakil ayrı spec, BR-9).
/// Sınıf isteğe bağlıdır ve yalnızca aynı şubenin sınıfı olabilir. Okul numarası isteğe bağlı, okulda harf farkı gözetmeden
/// tekildir (<see cref="SchoolNumberKey"/>). Sağlık notu ve alerji yalnızca şifreli (<c>…Cipher</c>) tutulur — düz metin
/// bu varlığa hiç girmez. Öğrenci silinmez: "ayrıldı" yapılır (<see cref="MarkLeft"/>) ve geri alınabilir.
/// </summary>
public sealed class Student
{
    public const int FullNameMaxLength = 100;
    public const int SchoolNumberMaxLength = 20;
    public const int HealthNoteMaxLength = 1000;
    public const int AllergyMaxLength = 500;

    private Student(Guid id, Guid tenantId, Guid branchId, string fullName, DateOnly birthDate, DateTimeOffset createdAt, Guid createdBy)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.FullName = fullName;
        this.BirthDate = birthDate;
        this.CreatedAt = createdAt;
        this.CreatedBy = createdBy;
        this.UpdatedAt = createdAt;
        this.UpdatedBy = createdBy;
        this.IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid? ClassroomId { get; private set; }

    public string FullName { get; private set; }

    public DateOnly BirthDate { get; private set; }

    public string? SchoolNumber { get; private set; }

    public string? SchoolNumberKey { get; private set; }

    public string? HealthNoteCipher { get; private set; }

    public string? AllergyCipher { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public static Student Create(Guid tenantId, Guid branchId, Guid? classroomId, string fullName, DateOnly birthDate, string? schoolNumber, DateTimeOffset now, Guid actorId)
    {
        Student student = new(Guid.CreateVersion7(now), tenantId, branchId, fullName.Trim(), birthDate, now, actorId) { ClassroomId = classroomId };
        student.SetSchoolNumber(schoolNumber);
        return student;
    }

    /// <summary>Okul numarasının karşılaştırma biçimi (boşsa <c>null</c>): kırpılmış, küçük harf (değişmez kültür).</summary>
    public static string? NormalizeSchoolNumber(string? schoolNumber) =>
        string.IsNullOrWhiteSpace(schoolNumber) ? null : schoolNumber.Trim().ToLowerInvariant();

    public void UpdateDetails(string fullName, DateOnly birthDate, string? schoolNumber, DateTimeOffset now, Guid actorId)
    {
        this.FullName = fullName.Trim();
        this.BirthDate = birthDate;
        this.SetSchoolNumber(schoolNumber);
        this.Touch(now, actorId);
    }

    public void ChangeClassroom(Guid? classroomId, DateTimeOffset now, Guid actorId)
    {
        this.ClassroomId = classroomId;
        this.Touch(now, actorId);
    }

    /// <summary>Sağlık alanlarını şifreli biçimleriyle yazar (boş alan <c>null</c>).</summary>
    public void SetHealth(string? healthNoteCipher, string? allergyCipher, DateTimeOffset now, Guid actorId)
    {
        this.HealthNoteCipher = healthNoteCipher;
        this.AllergyCipher = allergyCipher;
        this.Touch(now, actorId);
    }

    public void MarkLeft(DateTimeOffset now, Guid actorId)
    {
        this.IsActive = false;
        this.Touch(now, actorId);
    }

    public void Reinstate(DateTimeOffset now, Guid actorId)
    {
        this.IsActive = true;
        this.Touch(now, actorId);
    }

    private void SetSchoolNumber(string? schoolNumber)
    {
        this.SchoolNumber = string.IsNullOrWhiteSpace(schoolNumber) ? null : schoolNumber.Trim();
        this.SchoolNumberKey = NormalizeSchoolNumber(schoolNumber);
    }

    private void Touch(DateTimeOffset now, Guid actorId)
    {
        this.UpdatedAt = now;
        this.UpdatedBy = actorId;
    }
}
