using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Online üretilen ikili dosyaların (online TTS sesi, online görsel) geçici deposu: <c>App_Data/artifacts/{jobId}/</c>.
/// Production modülü <see cref="SaveAsync"/> ile kaydeder ve dönen <see cref="ArtifactDownload"/>'ı <c>StageCommand.Artifacts</c>'a koyar;
/// ajan <c>GET api/agents/artifacts/{id}</c> ile indirir, <c>DELETE api/agents/artifacts/{id}</c> ile onaylar (dosya silinir).
/// İş bitince <see cref="DeleteJobArtifactsAsync"/> kalanları temizler.
/// </summary>
public interface IArtifactStore
{
    /// <summary>Akışı kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="name">Mantıksal ad (ör. <c>scene-2.wav</c>).</param>
    /// <param name="contentType">İçerik türü.</param>
    /// <param name="content">İçerik akışı.</param>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İndirme bilgisi (göreli adres <c>api/agents/artifacts/{id}</c>).</returns>
    Task<Result<ArtifactDownload>> SaveAsync(
        Guid userId,
        Guid jobId,
        string name,
        string? contentType,
        Stream content,
        int? sceneIndex = null,
        CancellationToken cancellationToken = default);

    /// <summary>Artefaktı okumak için açar.</summary>
    /// <param name="userId">Kullanıcı (sahiplik kontrolü).</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İçerik.</returns>
    Task<Result<ArtifactContent>> OpenAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default);

    /// <summary>Artefaktı siler (ajan onayı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DeleteAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default);

    /// <summary>İşin tüm artefaktlarını siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    Task<Result<int>> DeleteJobArtifactsAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
}
