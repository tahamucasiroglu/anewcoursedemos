namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Profil başına üretim ayarları (spec 0006 Req 1–7): altyazı, ilerleme çubuğu, video kodu modeli + efor, video motoru, görsel modeli
/// (<c>null</c> = "Kapalı — koddan çiz") ve ses modeli (<c>null</c> = "Kapalı — sessiz video"). Satır yoksa varsayılanlar geçerlidir.
/// </summary>
public sealed record ProductionSettings
{
    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets profil (birincil anahtar).</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets a value indicating whether altyazı (ses kapalıyken saklanır ama uygulanmaz; Req 5).</summary>
    public bool Subtitles { get; init; }

    /// <summary>Gets a value indicating whether ilerleme çubuğu.</summary>
    public bool ProgressBar { get; init; }

    /// <summary>Gets video kodu modeli; seçilmediyse <c>null</c>.</summary>
    public string? CodeModel { get; init; }

    /// <summary>Gets video kodu eforu.</summary>
    public string? CodeEffort { get; init; }

    /// <summary>Gets video motoru.</summary>
    public required string Engine { get; init; }

    /// <summary>Gets görsel modeli; <c>null</c> = "Kapalı — koddan çiz".</summary>
    public string? ImageModel { get; init; }

    /// <summary>Gets ses modeli; <c>null</c> = "Kapalı — sessiz video".</summary>
    public string? AudioModel { get; init; }

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
