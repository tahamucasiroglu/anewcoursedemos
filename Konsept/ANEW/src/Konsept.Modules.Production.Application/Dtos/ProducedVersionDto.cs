namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Videonun bir sürümü (spec 0007 Req 5–6): numara, durum (0006 Req 13 adı), önceki sürüme göre değişiklik özeti, online maliyet, modeller ve
/// motorlar (katalog adıyla; kapalıysa <c>null</c>), altyazı/ilerleme çubuğu, süre, biçim, başlangıç–bitiş, AI ses beyanı ve sürümün kendi
/// konsept metni.
/// </summary>
/// <param name="Id">Sürüm (video kaydı).</param>
/// <param name="Number">Sürüm numarası (1'den).</param>
/// <param name="Status">Durum anahtarı (<c>completed</c>).</param>
/// <param name="Changes">Değişiklik özeti ("İlk sürüm" dahil).</param>
/// <param name="CostUsd">Online maliyet (USD).</param>
/// <param name="DurationSeconds">Süre (sn).</param>
/// <param name="Format">Biçim (<c>landscape</c> / <c>portrait</c>).</param>
/// <param name="StartedAt">Üretim başlangıcı (UTC).</param>
/// <param name="EndedAt">Üretim bitişi (UTC).</param>
/// <param name="CodeModel">Video kodu modeli.</param>
/// <param name="Engine">Video motoru.</param>
/// <param name="ImageModel">Görsel modeli.</param>
/// <param name="AudioModel">Ses motoru.</param>
/// <param name="Subtitles">Altyazı.</param>
/// <param name="ProgressBar">İlerleme çubuğu.</param>
/// <param name="AiVoiceNotice">AI ses beyanı.</param>
/// <param name="ConceptTitle">Konsept başlığı (sürümün metni).</param>
/// <param name="Scenes">Sahneler, sırasıyla.</param>
public sealed record ProducedVersionDto(
    Guid Id,
    int Number,
    string Status,
    string Changes,
    decimal CostUsd,
    int DurationSeconds,
    string Format,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string? CodeModel,
    string Engine,
    string? ImageModel,
    string? AudioModel,
    bool Subtitles,
    bool ProgressBar,
    bool AiVoiceNotice,
    string ConceptTitle,
    IReadOnlyList<ProducedSceneDto> Scenes);
