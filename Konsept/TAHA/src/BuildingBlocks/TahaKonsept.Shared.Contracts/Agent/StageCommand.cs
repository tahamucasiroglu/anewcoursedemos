using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Sunucunun ajana gönderdiği "bu aşamayı çalıştır" komutu (<c>IAgentClient.ExecuteStage</c>).
/// Önemli alanlar: <see cref="JobId"/>, <see cref="Stage"/>, <see cref="SceneIndex"/> (sahne bazlı aşamalarda),
/// <see cref="PayloadJson"/> (aşamaya özgü girdi JSON'u), <see cref="Settings"/> (motor/model ayarları), <see cref="Artifacts"/> (indirilecek girdiler).
/// </summary>
public sealed record StageCommand
{
    /// <summary>Komut kimliği (ilerleme/sonuç raporları bununla eşleşir).</summary>
    public Guid CommandId { get; init; }

    /// <summary>Üretim işi kimliği.</summary>
    public Guid JobId { get; init; }

    /// <summary>Video kimliği (çalışma alanı klasörü için).</summary>
    public Guid VideoId { get; init; }

    /// <summary>Çalıştırılacak aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne indeksi (tüm video için <c>null</c>).</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Aşama girdi hash'i (SHA256; ajan önbellek için kullanabilir).</summary>
    public string InputHash { get; init; } = string.Empty;

    /// <summary>Aşamaya özgü girdi JSON'u.</summary>
    public string PayloadJson { get; init; } = "{}";

    /// <summary>Aşama ayarları (motor id, model id, ses, efor, altyazı açık/kapalı ...).</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    /// <summary>Çalıştırmadan önce indirilecek artefaktlar.</summary>
    public IReadOnlyList<ArtifactDownload> Artifacts { get; init; } = [];
}
