using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte ajan geçidi: çevrimiçi bayrağı, gönderilen komutlar (bekleyenler kuyruğu + tüm geçmiş), duraklat/iptal/sil çağrıları,
/// önizleme sonucu ve sabit oynatma adresleri.
/// </summary>
public sealed class FakeAgentGateway : IAgentGateway
{
    private static readonly Error Offline = Error.Conflict("agents.offline", "Masaüstü programı çevrimdışı.");

    /// <summary>Ajan çevrimiçi mi.</summary>
    public bool Online { get; set; } = true;

    /// <summary>Henüz yanıtlanmamış komutlar.</summary>
    public ConcurrentQueue<StageCommand> Pending { get; } = new();

    /// <summary>Tüm gönderilen komutlar.</summary>
    public ConcurrentQueue<StageCommand> Sent { get; } = new();

    /// <summary>Duraklatılan çalışma alanları.</summary>
    public ConcurrentQueue<Guid> Paused { get; } = new();

    /// <summary>İptal edilen çalışma alanları.</summary>
    public ConcurrentQueue<Guid> Cancelled { get; } = new();

    /// <summary>Silinen videolar.</summary>
    public ConcurrentQueue<Guid> DeletedVideos { get; } = new();

    /// <summary>Önizleme istekleri.</summary>
    public ConcurrentQueue<SpeechPreviewCommand> Previews { get; } = new();

    /// <inheritdoc />
    public bool IsAgentOnline(Guid userId) => Online;

    /// <inheritdoc />
    public Task<AgentDeviceInfo?> GetOnlineAgentAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Online ? new AgentDeviceInfo { Id = Guid.CreateVersion7(), MachineName = "TEST", IsOnline = true } : null);

    /// <inheritdoc />
    public Task<Result> SendStageAsync(Guid userId, StageCommand command, CancellationToken cancellationToken = default)
    {
        if (!Online)
        {
            return Task.FromResult(Result.Failure(Offline));
        }

        Pending.Enqueue(command);
        Sent.Enqueue(command);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> CancelJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        Cancelled.Enqueue(jobId);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> PauseJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        Paused.Enqueue(jobId);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> OpenFolderAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) => Done();

    /// <inheritdoc />
    public Task<Result> OpenInSystemPlayerAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) => Done();

    /// <inheritdoc />
    public Task<Result> DeleteVideoAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default)
    {
        DeletedVideos.Enqueue(videoId);
        return Done();
    }

    /// <inheritdoc />
    public Task<Result> RefreshCapabilitiesAsync(Guid userId, CancellationToken cancellationToken = default) => Done();

    /// <inheritdoc />
    public Task<Result<SpeechPreviewResult>> PreviewSpeechAsync(Guid userId, SpeechPreviewCommand command, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Previews.Enqueue(command);
        return Task.FromResult(Online
            ? Result.Success(new SpeechPreviewResult { RequestId = command.RequestId, Success = true, AudioPath = "/previews/" + command.RequestId + ".wav" })
            : Result.Failure<SpeechPreviewResult>(Offline));
    }

    /// <inheritdoc />
    public Result<string> GetVideoUrl(Guid userId, Guid videoId) =>
        Online ? Result.Success("http://192.168.1.10:7120/videos/" + videoId + "?k=key") : Result.Failure<string>(Offline);

    /// <inheritdoc />
    public Result<string> GetThumbnailUrl(Guid userId, Guid videoId) =>
        Online ? Result.Success("http://192.168.1.10:7120/thumbs/" + videoId + "?k=key") : Result.Failure<string>(Offline);

    /// <inheritdoc />
    public Result<string> GetAgentFileUrl(Guid userId, string relativePath) =>
        Online ? Result.Success("http://192.168.1.10:7120" + relativePath + "?k=key") : Result.Failure<string>(Offline);

    private Task<Result> Done() => Task.FromResult(Online ? Result.Success() : Result.Failure(Offline));
}
