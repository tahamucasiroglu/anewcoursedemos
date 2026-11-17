namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Tamamlanan videonun sürüm kaydı (spec 0017 Req 22): konsept metninin üretim anındaki kopyası, kullanılan modeller ve motor, altyazı/ilerleme
/// çubuğu seçimi, AI ses notu, başlangıç–bitiş. İçerik sonradan değişmez (yalnızca silme işareti <see cref="DeletedAt"/> yazılır — spec 0019); video dosyası ajandadır (yalnızca adı burada).
/// </summary>
public sealed record ProducedVideo
{
    /// <summary>Gets kimlik (UUIDv7).</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Gets üreten iş.</summary>
    public Guid JobId { get; init; }

    /// <summary>Gets başlık.</summary>
    public required string Title { get; init; }

    /// <summary>Gets konsept metninin kopyası (JSON).</summary>
    public required string ConceptText { get; init; }

    /// <summary>Gets video kodu modeli.</summary>
    public string? CodeModel { get; init; }

    /// <summary>Gets görsel modeli (<c>null</c> = kapalı).</summary>
    public string? ImageModel { get; init; }

    /// <summary>Gets ses modeli (<c>null</c> = sessiz).</summary>
    public string? AudioModel { get; init; }

    /// <summary>Gets video motoru.</summary>
    public required string Engine { get; init; }

    /// <summary>Gets a value indicating whether altyazı.</summary>
    public bool Subtitles { get; init; }

    /// <summary>Gets a value indicating whether ilerleme çubuğu.</summary>
    public bool ProgressBar { get; init; }

    /// <summary>Gets a value indicating whether AI ses notu yazıldı.</summary>
    public bool AiVoiceNotice { get; init; }

    /// <summary>Gets ajan klasöründeki dosya adı.</summary>
    public required string FileName { get; init; }

    /// <summary>Gets başlangıç (UTC).</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Gets bitiş (UTC).</summary>
    public DateTimeOffset EndedAt { get; init; }

    /// <summary>Gets video süresi (sn; sahne planı toplamı — spec 0007 Req 1).</summary>
    public int DurationSeconds { get; init; }

    /// <summary>Gets biçim (<c>landscape</c> / <c>portrait</c>).</summary>
    public string Format { get; init; } = "landscape";

    /// <summary>Gets sürümün online model maliyeti (USD; yerel aşamalar ücretsiz — spec 0007 Req 5, BR-12).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Gets silinme anı (UTC; <c>null</c> = silinmemiş). Sil videonun tüm sürümlerine aynı anı yazar (spec 0019 Req 1, BR-7 soft delete).</summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
