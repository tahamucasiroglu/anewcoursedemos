namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Duyurunun alıcı velisi (spec 0011 C-3): yayın anında belirlenir, veli başına tek satır (iki çocuğu kapsamda olsa da).
/// İlk okuma zamanı yalnızca ilk açışta yazılır (<see cref="MarkRead"/>); ikinci açış değiştirmez.
/// </summary>
public sealed class AnnouncementRecipient
{
    private AnnouncementRecipient(Guid id, Guid tenantId, Guid announcementId, Guid guardianUserId)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.AnnouncementId = announcementId;
        this.GuardianUserId = guardianUserId;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AnnouncementId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static AnnouncementRecipient For(Announcement announcement, Guid guardianUserId)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        return new(Guid.CreateVersion7(announcement.PublishedAt), announcement.TenantId, announcement.Id, guardianUserId);
    }

    /// <summary>İlk okumayı kaydeder; daha önce okunduysa hiçbir şey değişmez. İlk okuma ise <c>true</c>.</summary>
    public bool MarkRead(DateTimeOffset now)
    {
        if (this.ReadAt is not null)
        {
            return false;
        }

        this.ReadAt = now;
        return true;
    }
}
