namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Sınıf öğretmeni ataması (spec 0010 C-3): kurucu bir sınıfa, o sınıfın şubesinde Öğretmen rolü olan kişiyi atar.
/// Öğretmen yalnızca atandığı sınıfların yoklamasını alır. Aynı kişi aynı sınıfa bir kez atanır; atama kaldırılınca
/// satır silinir (geçmişi audit kaydında kalır). Şube, sınıfın şubesidir (filtre ve sorgu kolaylığı için tutulur).
/// </summary>
public sealed class ClassroomTeacher
{
    private ClassroomTeacher(Guid id, Guid tenantId, Guid branchId, Guid classroomId, Guid teacherUserId, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.ClassroomId = classroomId;
        this.TeacherUserId = teacherUserId;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid ClassroomId { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ClassroomTeacher Assign(Guid tenantId, Guid branchId, Guid classroomId, Guid teacherUserId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, branchId, classroomId, teacherUserId, now);
}
