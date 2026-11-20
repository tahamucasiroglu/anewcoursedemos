namespace OkulYonetim.Domain.Attendance;

/// <summary>
/// Bir sınıfın bir günlük yoklaması (spec 0010 C-1): kurum, şube, sınıf, gün (Türkiye günü), son kaydeden kişi, ilk kayıt ve
/// son güncelleme zamanı. Sınıf + gün tekildir; aynı gün yeniden kaydetmek bu satırı günceller (düzeltme). Öğrenci
/// durumları <see cref="AttendanceEntry"/> satırlarındadır. <see cref="Revision"/> her kayıtta bir artar (ilk kayıt 1):
/// ekranın açıldığı sürüm kaydetmede geri gelir, arada başkası kaydettiyse eski ekran üzerine yazamaz (iyimser eşzamanlılık,
/// 0010 review F-1).
/// </summary>
public sealed class AttendanceSheet
{
    private AttendanceSheet(Guid id, Guid tenantId, Guid branchId, Guid classroomId, DateOnly day, Guid recordedBy, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.ClassroomId = classroomId;
        this.Day = day;
        this.RecordedBy = recordedBy;
        this.CreatedAt = createdAt;
        this.UpdatedAt = updatedAt;
        this.Revision = 1;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid ClassroomId { get; private set; }

    public DateOnly Day { get; private set; }

    public Guid RecordedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int Revision { get; private set; }

    public static AttendanceSheet Start(Guid tenantId, Guid branchId, Guid classroomId, DateOnly day, Guid recordedBy, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, branchId, classroomId, day, recordedBy, now, now);

    public void Touch(Guid recordedBy, DateTimeOffset now)
    {
        this.RecordedBy = recordedBy;
        this.UpdatedAt = now;
        this.Revision++;
    }
}
