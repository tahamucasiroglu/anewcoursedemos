namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Yayın sonucu: duyuru kimliği ve alıcı veli sayısı.</summary>
public sealed record AnnouncementPublished(Guid Id, int RecipientCount);
