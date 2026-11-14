namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın aşamayı çalıştırmadan önce indirmesi gereken girdi artefaktı (ör. online üretilmiş ses/görsel).
/// İndirme: <c>GET api/agents/artifacts/{id}</c> (JWT ile).
/// </summary>
public sealed record ArtifactDownload
{
    /// <summary>Artefakt kimliği.</summary>
    public Guid ArtifactId { get; init; }

    /// <summary>Mantıksal ad (ör. <c>scene-3.png</c>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>İndirme adresi (API'ye göre göreli ya da mutlak).</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>İçerik türü.</summary>
    public string? ContentType { get; init; }

    /// <summary>Sahne indeksi (sahneye özgüyse).</summary>
    public int? SceneIndex { get; init; }
}
