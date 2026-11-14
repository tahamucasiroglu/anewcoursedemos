using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Agents.Domain.Entities;

/// <summary>
/// Online üretilip ajanın indirmesini bekleyen geçici ikili dosyanın kaydı (dosya <c>App_Data/artifacts/{jobId}/</c> altında).
/// Ajan indirip onaylayınca (ack) dosya ve kayıt silinir. Önemli alanlar: <see cref="JobId"/>, <see cref="RelativePath"/>, <see cref="ContentType"/>.
/// </summary>
public sealed class StoredArtifact : AggregateRoot, IAuditable, IHasUser
{
    private StoredArtifact()
    {
    }

    private StoredArtifact(Guid id)
        : base(id)
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Üretim işi.</summary>
    public Guid JobId { get; private set; }

    /// <summary>Mantıksal ad (ör. <c>scene-3.png</c>).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>İçerik türü.</summary>
    public string ContentType { get; private set; } = "application/octet-stream";

    /// <summary>Artefakt köküne göre göreli dosya yolu (<c>/</c> ayraçlı).</summary>
    public string RelativePath { get; private set; } = string.Empty;

    /// <summary>Boyut (bayt).</summary>
    public long SizeBytes { get; private set; }

    /// <summary>Sahne indeksi.</summary>
    public int? SceneIndex { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Kayıt oluşturur.</summary>
    /// <param name="id">Kimlik (dosya adında da kullanılır).</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="name">Mantıksal ad.</param>
    /// <param name="contentType">İçerik türü.</param>
    /// <param name="relativePath">Göreli yol.</param>
    /// <param name="sizeBytes">Boyut.</param>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <returns>Kayıt.</returns>
    public static StoredArtifact Create(
        Guid id,
        Guid userId,
        Guid jobId,
        string name,
        string? contentType,
        string relativePath,
        long sizeBytes,
        int? sceneIndex) =>
        new(id)
        {
            UserId = userId,
            JobId = jobId,
            Name = name,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            RelativePath = relativePath,
            SizeBytes = sizeBytes,
            SceneIndex = sceneIndex,
        };
}
