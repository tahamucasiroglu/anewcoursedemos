namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Profil oluşturma/düzenleme isteği (spec 0003 Req 2). Alanlar sunucuda da denetlenir (Req 6).</summary>
/// <param name="ChannelName">Kanal adı.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Biçim.</param>
/// <param name="DurationSeconds">Hedef süre (saniye).</param>
/// <param name="Content">Kanal içeriği.</param>
public sealed record SaveProfileRequest(string? ChannelName, string? Language, string? Format, int? DurationSeconds, string? Content);
