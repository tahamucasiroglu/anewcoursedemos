namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyurunun alıcı velisi (sonuç ekranı için depo satırı): kişi, ad-soyad ve ilk okuma zamanı.</summary>
public sealed record RecipientRow(Guid GuardianUserId, string GuardianName, DateTimeOffset? ReadAt);
