namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Duyurunun yayın anında kapsadığı ve en az bir alıcı velisi olan öğrenci (spec 0011): onay formunun öğrenci listesi ve
/// velinin akışındaki "ilgili çocuk" bu satırlardan gelir. Sonradan sınıfa giren öğrenci eklenmez.
/// </summary>
public sealed class AnnouncementStudent
{
    private AnnouncementStudent(Guid id, Guid tenantId, Guid announcementId, Guid studentId)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AnnouncementId = announcementId;
        this.StudentId = studentId;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AnnouncementId { get; private set; }

    public Guid StudentId { get; private set; }

    public static AnnouncementStudent For(Announcement announcement, Guid studentId)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        return new(Guid.CreateVersion7(announcement.PublishedAt), announcement.TenantId, announcement.Id, studentId);
    }
}
