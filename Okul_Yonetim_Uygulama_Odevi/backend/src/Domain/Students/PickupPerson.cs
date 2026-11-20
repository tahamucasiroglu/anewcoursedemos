namespace OkulYonetim.Domain.Students;

/// <summary>
/// Öğrenciyi okuldan teslim alabilecek kişi (spec 0009 C-5): hesap değil, kartta bir satır — ad-soyad, yakınlık (serbest
/// metin) ve telefon (E.164, <c>Identity/PhoneNumber</c> kuralıyla). Bir öğrencide en fazla <see cref="MaxPerStudent"/> kişi;
/// liste sırası <c>Position</c> ile korunur.
/// </summary>
public sealed class PickupPerson
{
    public const int MaxPerStudent = 10;
    public const int FullNameMaxLength = 100;
    public const int RelationshipMaxLength = 50;

    private PickupPerson(Guid id, Guid tenantId, Guid studentId, int position, string fullName, string relationship, string phone)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.StudentId = studentId;
        this.Position = position;
        this.FullName = fullName;
        this.Relationship = relationship;
        this.Phone = phone;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid StudentId { get; private set; }

    public int Position { get; private set; }

    public string FullName { get; private set; }

    public string Relationship { get; private set; }

    public string Phone { get; private set; }

    public static PickupPerson Create(Guid tenantId, Guid studentId, int position, string fullName, string relationship, string normalizedPhone, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, studentId, position, fullName.Trim(), relationship.Trim(), normalizedPhone);
}
