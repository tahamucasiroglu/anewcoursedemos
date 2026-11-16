namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Ayarların tamamı (spec 0006 Req 7): arayüz her değişiklikte bütün ayarı gönderir; görsel/ses <c>null</c> = "Kapalı".</summary>
public sealed record SaveProductionSettingsRequest
{
    /// <summary>Gets a value indicating whether altyazı.</summary>
    public bool Subtitles { get; init; }

    /// <summary>Gets a value indicating whether ilerleme çubuğu.</summary>
    public bool ProgressBar { get; init; }

    /// <summary>Gets video kodu modeli.</summary>
    public string? CodeModel { get; init; }

    /// <summary>Gets video kodu eforu.</summary>
    public string? CodeEffort { get; init; }

    /// <summary>Gets video motoru.</summary>
    public string? Engine { get; init; }

    /// <summary>Gets görsel modeli.</summary>
    public string? ImageModel { get; init; }

    /// <summary>Gets ses modeli.</summary>
    public string? AudioModel { get; init; }
}
