namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Hazır şablon (spec 0003 Req 5).</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Title">Görünen ad.</param>
/// <param name="ChannelName">Örnek kanal adı.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Biçim.</param>
/// <param name="DurationSeconds">Hedef süre (saniye).</param>
/// <param name="Content">Örnek kanal içeriği.</param>
public sealed record ProfileTemplateDto(string Id, string Title, string ChannelName, string Language, string Format, int DurationSeconds, string Content);
