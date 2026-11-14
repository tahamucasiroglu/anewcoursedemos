using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Bellek içi sahte artefakt deposu (kaydedilenleri ve silinen işleri tutar).
/// </summary>
public sealed class FakeArtifactStore : IArtifactStore
{
    /// <summary>Kaydedilen artefaktlar (kimlik → ad, iş, boyut).</summary>
    public ConcurrentDictionary<Guid, (string Name, Guid JobId, int Length)> Saved { get; } = new();

    /// <summary>Artefaktları silinen işler.</summary>
    public ConcurrentQueue<Guid> DeletedJobs { get; } = new();

    /// <inheritdoc />
    public async Task<Result<ArtifactDownload>> SaveAsync(Guid userId, Guid jobId, string name, string? contentType, Stream content, int? sceneIndex = null, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var id = Guid.CreateVersion7();
        Saved[id] = (name, jobId, (int)memory.Length);
        return new ArtifactDownload { ArtifactId = id, Name = name, Url = "api/agents/artifacts/" + id, ContentType = contentType, SceneIndex = sceneIndex };
    }

    /// <inheritdoc />
    public Task<Result<ArtifactContent>> OpenAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<ArtifactContent>(Error.NotFound("fake.not_found", "Yok")));

    /// <inheritdoc />
    public Task<Result> DeleteAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default)
    {
        Saved.TryRemove(artifactId, out _);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<int>> DeleteJobArtifactsAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        DeletedJobs.Enqueue(jobId);
        var removed = Saved.Where(p => p.Value.JobId == jobId).Select(p => p.Key).ToList();
        foreach (var id in removed)
        {
            Saved.TryRemove(id, out _);
        }

        return Task.FromResult(Result.Success(removed.Count));
    }
}
