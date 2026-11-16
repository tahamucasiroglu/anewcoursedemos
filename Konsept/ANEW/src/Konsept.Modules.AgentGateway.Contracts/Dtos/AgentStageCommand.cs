namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanda çalışacak bir yerel üretim aşaması (spec 0017 Req 13): iş, çalışma klasörü anahtarı (konsept), aşama, işe sabitlenen ayarlar ve
/// sahneler. Birleştirmede videonun kimliği ve dosya adı da gelir. Komut hiçbir zaman API key taşımaz.
/// </summary>
public sealed record AgentStageCommand
{
    /// <summary>Aşama: görseller.</summary>
    public const string Images = "images";

    /// <summary>Aşama: seslendirme.</summary>
    public const string Voice = "voice";

    /// <summary>Aşama: render.</summary>
    public const string Render = "render";

    /// <summary>Aşama: birleştirme.</summary>
    public const string Merge = "merge";

    /// <summary>Gets iş.</summary>
    public Guid JobId { get; init; }

    /// <summary>Gets çalışma klasörü anahtarı (konsept kimliği; yeniden sıraya alınan iş aynı klasörü kullanır).</summary>
    public Guid WorkKey { get; init; }

    /// <summary>Gets aşama.</summary>
    public required string Stage { get; init; }

    /// <summary>Gets a value indicating whether altyazı.</summary>
    public bool Subtitles { get; init; }

    /// <summary>Gets a value indicating whether ilerleme çubuğu.</summary>
    public bool ProgressBar { get; init; }

    /// <summary>Gets video motoru.</summary>
    public required string Engine { get; init; }

    /// <summary>Gets görsel modeli (<c>null</c> = kapalı).</summary>
    public string? ImageModel { get; init; }

    /// <summary>Gets ses modeli (<c>null</c> = sessiz).</summary>
    public string? AudioModel { get; init; }

    /// <summary>Gets video dili.</summary>
    public required string Language { get; init; }

    /// <summary>Gets biçim (<c>landscape</c> · <c>portrait</c>).</summary>
    public required string Format { get; init; }

    /// <summary>Gets sahneler.</summary>
    public IReadOnlyList<AgentStageScene> Scenes { get; init; } = [];

    /// <summary>Gets birleştirmede videonun kimliği.</summary>
    public Guid? VideoId { get; init; }

    /// <summary>Gets birleştirmede video dosyasının adı.</summary>
    public string? VideoFileName { get; init; }
}
