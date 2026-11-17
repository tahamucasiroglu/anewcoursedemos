using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Ajan komut kanallarının temeli: bağlantı kimliğini doğrular, gönderimi alt sınıfa bırakır.
/// </summary>
public abstract class AgentChannelBase : IAgentChannel
{
    /// <inheritdoc />
    public Task<AgentPingReply> PingAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokePingAsync(connectionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentToolCheckReply> CheckToolsAsync(string connectionId, AgentToolCheckRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokeAsync<AgentToolCheckReply>(connectionId, AgentHubContract.CheckTools, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentStageReply> RunStageAsync(string connectionId, AgentStageCommand command, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokeAsync<AgentStageReply>(connectionId, AgentHubContract.RunStage, command, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelStageAsync(string connectionId, Guid jobId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return SendAsync(connectionId, AgentHubContract.CancelStage, jobId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentStreamReply> OpenStreamAsync(string connectionId, AgentStreamRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokeAsync<AgentStreamReply>(connectionId, AgentHubContract.OpenStream, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentFileActionReply> OpenFileAsync(string connectionId, AgentFileActionRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokeAsync<AgentFileActionReply>(connectionId, AgentHubContract.OpenFile, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentDeleteFilesReply> DeleteFilesAsync(string connectionId, AgentDeleteFilesRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokeAsync<AgentDeleteFilesReply>(connectionId, AgentHubContract.DeleteFiles, request, cancellationToken);
    }

    /// <summary>Komutu gönderir ve cevabı bekler.</summary>
    /// <param name="connectionId">Ajan bağlantısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajanın cevabı.</returns>
    protected abstract Task<AgentPingReply> InvokePingAsync(string connectionId, CancellationToken cancellationToken);

    /// <summary>Tek argümanlı komutu gönderir ve cevabı bekler (SignalR client result).</summary>
    /// <typeparam name="T">Cevap türü.</typeparam>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="method">Metot adı.</param>
    /// <param name="argument">Argüman.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<T> InvokeAsync<T>(string connectionId, string method, object argument, CancellationToken cancellationToken);

    /// <summary>Cevapsız komut gönderir.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="method">Metot adı.</param>
    /// <param name="argument">Argüman.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task SendAsync(string connectionId, string method, object argument, CancellationToken cancellationToken);
}
