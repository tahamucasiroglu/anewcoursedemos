namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Profil oluşturma/düzenleme gövdesi (tarayıcıdan gelir, Api'ye aynen iletilir; kurallar Api'de de uygulanır).</summary>
/// <param name="ChannelName">Kanal adı.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Biçim.</param>
/// <param name="DurationSeconds">Hedef süre (saniye).</param>
/// <param name="Content">Kanal içeriği.</param>
public sealed record SaveProfileApiRequest(string? ChannelName, string? Language, string? Format, int? DurationSeconds, string? Content);
