namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Sınıf (şube-sınıf eşleşmesi, spec 0008 C-1): bir dönemde, bir şubede, bir seviyede açılan öğrenci grubu
/// ("Bahçelievler 5-A"). Dönemi ve şubesi kurulduktan sonra değişmez (yanlışsa pasifleştirilip doğrusu açılır); adı ve
/// seviyesi düzenlenir. Silinmez, pasifleştirilir. Sonraki spec'ler (öğrenci, yoklama) sınıfa kimliğiyle bağlanır.
/// </summary>
public sealed class Classroom
{
    private Classroom(Guid id, Guid tenantId, Guid termId, Guid branchId, Guid gradeLevelId, string name, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.TermId = termId;
        this.BranchId = branchId;
        this.GradeLevelId = gradeLevelId;
        this.Name = name;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid TermId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid GradeLevelId { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Classroom Create(Guid tenantId, Guid termId, Guid branchId, Guid gradeLevelId, string name, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, termId, branchId, gradeLevelId, name.Trim(), now);

    public void Update(string name, Guid gradeLevelId)
    {
        this.Name = name.Trim();
        this.GradeLevelId = gradeLevelId;
    }

    public void Deactivate() => this.IsActive = false;

    public void Activate() => this.IsActive = true;
}
