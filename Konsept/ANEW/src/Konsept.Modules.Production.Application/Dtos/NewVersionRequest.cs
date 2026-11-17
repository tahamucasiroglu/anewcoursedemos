namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Yeni sürüm isteği (spec 0020 Req 1–3): taban sürüm ve yeni ayarlar (motor, görsel modeli, ses modeli — <c>null</c> = sessiz, altyazı,
/// ilerleme çubuğu). Online çağrı gerekiyorsa <paramref name="ConfirmOnline"/> olmadan iş oluşmaz.
/// </summary>
/// <param name="BaseVersionId">Taban sürüm (video kaydı).</param>
/// <param name="Engine">Video motoru.</param>
/// <param name="ImageModel">Görsel modeli.</param>
/// <param name="AudioModel">Ses modeli; <c>null</c> = sessiz video.</param>
/// <param name="Subtitles">Altyazı.</param>
/// <param name="ProgressBar">İlerleme çubuğu.</param>
/// <param name="ConfirmOnline">Online çağrı uyarısı onaylandı.</param>
public sealed record NewVersionRequest(
    Guid BaseVersionId, string? Engine, string? ImageModel, string? AudioModel, bool Subtitles, bool ProgressBar, bool ConfirmOnline = false);
