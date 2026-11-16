namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Kullanıcıya dönen profil (spec 0003 Req 2). Sistem talimatı taşınmaz (Req 16).</summary>
/// <param name="Id">Profil kimliği.</param>
/// <param name="ChannelName">Kanal adı.</param>
/// <param name="Language">Video dili (<c>tr</c> · <c>en</c>).</param>
/// <param name="Format">Biçim (<c>landscape</c> · <c>portrait</c>).</param>
/// <param name="DurationSeconds">Hedef süre (saniye).</param>
/// <param name="Content">Kanal içeriği.</param>
/// <param name="CreatedAt">Oluşturulma (UTC).</param>
/// <param name="CanDelete">Silinebilir mi (konsepti yoksa; konsept 0014'te gelir, Req 14).</param>
public sealed record ProfileDto(Guid Id, string ChannelName, string Language, string Format, int DurationSeconds, string Content, DateTimeOffset CreatedAt, bool CanDelete);
