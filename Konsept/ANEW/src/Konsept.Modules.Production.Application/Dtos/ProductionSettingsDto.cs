namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Profilin üretim ayarları (spec 0006 Req 1–7); görsel/ses <c>null</c> = "Kapalı".</summary>
/// <param name="Subtitles">Altyazı (kayıtlı değer; ses kapalıyken arayüzde pasif).</param>
/// <param name="ProgressBar">İlerleme çubuğu.</param>
/// <param name="CodeModel">Video kodu modeli.</param>
/// <param name="CodeEffort">Video kodu eforu.</param>
/// <param name="Engine">Video motoru.</param>
/// <param name="ImageModel">Görsel modeli ya da <c>null</c>.</param>
/// <param name="AudioModel">Ses modeli ya da <c>null</c>.</param>
public sealed record ProductionSettingsDto(
    bool Subtitles, bool ProgressBar, string? CodeModel, string? CodeEffort, string Engine, string? ImageModel, string? AudioModel);
