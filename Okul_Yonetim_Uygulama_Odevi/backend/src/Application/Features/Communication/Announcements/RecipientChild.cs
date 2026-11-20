namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Alıcı velinin, duyurunun kapsadığı ve bugün de bağlı olduğu çocuğu (okumayanlar listesinde çocuk adı için).</summary>
public sealed record RecipientChild(Guid GuardianUserId, string StudentName);
