namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Yeni sürüm alanı (spec 0020 Req 1–2): taban sürüm, onun ayarları ve seçilebilen motor/görsel/ses modelleri (katalogdan; Remotion yalnızca
/// ajanda kuruluysa; yetkisiz model yok). Görsel kapalı tabanda görsel modeli değiştirilemez.
/// </summary>
/// <param name="BaseVersionId">Taban sürüm.</param>
/// <param name="Engine">Taban motor.</param>
/// <param name="ImageModel">Taban görsel modeli.</param>
/// <param name="AudioModel">Taban ses modeli.</param>
/// <param name="Subtitles">Taban altyazı.</param>
/// <param name="ProgressBar">Taban ilerleme çubuğu.</param>
/// <param name="Engines">Motorlar.</param>
/// <param name="Images">Görsel modelleri.</param>
/// <param name="Audios">Ses modelleri.</param>
public sealed record VersionOptionsDto(
    Guid BaseVersionId,
    string Engine,
    string? ImageModel,
    string? AudioModel,
    bool Subtitles,
    bool ProgressBar,
    IReadOnlyList<VersionOptionDto> Engines,
    IReadOnlyList<VersionOptionDto> Images,
    IReadOnlyList<VersionOptionDto> Audios);
