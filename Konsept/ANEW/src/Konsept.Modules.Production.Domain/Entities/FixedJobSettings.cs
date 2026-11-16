namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// İş <i>Üretiliyor</i>'a geçtiği anda işe sabitlenen değerler (spec 0006 Req 7, 20): üretim ayarları ve konseptin <b>kaydındaki</b> dil,
/// biçim, hedef süre. Sonradan profil ayarı ya da profil değişse de bu iş etkilenmez.
/// </summary>
public sealed record FixedJobSettings
{
    /// <summary>Gets a value indicating whether altyazı (ses kapalıysa her zaman <c>false</c>).</summary>
    public bool Subtitles { get; init; }

    /// <summary>Gets a value indicating whether ilerleme çubuğu.</summary>
    public bool ProgressBar { get; init; }

    /// <summary>Gets video kodu modeli.</summary>
    public string? CodeModel { get; init; }

    /// <summary>Gets video kodu eforu.</summary>
    public string? CodeEffort { get; init; }

    /// <summary>Gets video motoru.</summary>
    public required string Engine { get; init; }

    /// <summary>Gets görsel modeli; <c>null</c> = "Kapalı — koddan çiz".</summary>
    public string? ImageModel { get; init; }

    /// <summary>Gets ses modeli; <c>null</c> = "Kapalı — sessiz video".</summary>
    public string? AudioModel { get; init; }

    /// <summary>Gets konseptin kaydındaki video dili.</summary>
    public required string Language { get; init; }

    /// <summary>Gets konseptin kaydındaki biçim.</summary>
    public required string Format { get; init; }

    /// <summary>Gets konseptin kaydındaki hedef süre (saniye).</summary>
    public int TargetDurationSeconds { get; init; }

    /// <summary>Gets sabitlenme zamanı (UTC).</summary>
    public DateTimeOffset FixedAt { get; init; }
}
