namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın bir aşamada ürettiği çıktı artefaktının meta verisi. İkili dosya ajanda kalır; Pi'ye yalnızca bu bilgi gider.
/// Metin/JSON çıktılar <see cref="InlineContent"/> içinde taşınabilir.
/// </summary>
public sealed record ProducedArtifact
{
    /// <summary>Mantıksal ad (ör. <c>scene-2.mp4</c>, <c>speech-1.wav</c>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Artefakt türü (ör. <c>audio</c>, <c>video</c>, <c>image</c>, <c>json</c>, <c>subtitle</c>).</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Sahne indeksi (sahneye özgüyse).</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Ajandaki yerel dosya yolu (çalışma alanına göre göreli).</summary>
    public string? LocalPath { get; init; }

    /// <summary>Dosya boyutu (bayt).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Süre (saniye; ses/video için).</summary>
    public double? DurationSec { get; init; }

    /// <summary>İçerik SHA256 (hex).</summary>
    public string? Sha256 { get; init; }

    /// <summary>Küçük metin/JSON içerik (ör. hizalama sonucu).</summary>
    public string? InlineContent { get; init; }

    /// <summary>Ek meta veriler.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
