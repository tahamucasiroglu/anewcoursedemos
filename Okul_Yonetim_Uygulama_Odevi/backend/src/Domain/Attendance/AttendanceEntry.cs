namespace OkulYonetim.Domain.Attendance;

/// <summary>Yoklamada bir öğrencinin durumu (spec 0010): yoklama + öğrenci tekil; düzeltmede durum değişir.</summary>
public sealed class AttendanceEntry
{
    private AttendanceEntry(Guid id, Guid tenantId, Guid sheetId, Guid studentId, AttendanceStatus status)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.SheetId = sheetId;
        this.StudentId = studentId;
        this.Status = status;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid SheetId { get; private set; }

    public Guid StudentId { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public static AttendanceEntry Create(AttendanceSheet sheet, Guid studentId, AttendanceStatus status, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return new(Guid.CreateVersion7(now), sheet.TenantId, sheet.Id, studentId, status);
    }

    public void Change(AttendanceStatus status) => this.Status = status;
}
