using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// <see cref="IAgentCommands"/> (spec 0017): komutu yalnızca kullanıcının kayıttaki geçerli ajan oturumuna verir (başka kullanıcının ya da
/// iptal edilmiş eski ajanın bağlantısı hiçbir zaman seçilmez — AC-17, AC-30). Ajan yoksa ya da gönderim sırasında bağlantı koparsa
/// <c>null</c>; çağıranın iptali yayılır. Bağlantı kopması dışındaki hata (ajan işleyicisi/sözleşme uyumsuzluğu) "ajan yok" sayılmaz: aşama
/// <see cref="AgentStageReply.Failed"/>, araç denetimi eksik <see cref="AgentProtocol"/> döner (review r1 M-3: sonsuz bekleme/ücretli tekrar yok).
/// </summary>
/// <param name="registry">Ajan oturum kaydı.</param>
/// <param name="channel">Komut kanalı.</param>
public abstract class AgentCommandsBase(IAgentConnectionRegistry registry, IAgentChannel channel) : IAgentCommands
{
    /// <summary>Ajan sözleşmeye uymadığında araç denetiminde eksik bildirilen kimlik (ajan güncellenmeli).</summary>
    public const string AgentProtocol = "agent";

    /// <inheritdoc />
    public Guid? ConnectedDevice(Guid userId) => registry.Find(userId)?.DeviceId;

    /// <inheritdoc />
    public Task<AgentToolCheckReply?> CheckToolsAsync(Guid userId, AgentToolCheckRequest request, CancellationToken cancellationToken) =>
        SendAsync(userId, (id, ct) => channel.CheckToolsAsync(id, request, ct), new AgentToolCheckReply([AgentProtocol]), cancellationToken);

    /// <inheritdoc />
    public Task<AgentStageReply?> RunStageAsync(Guid userId, AgentStageCommand command, CancellationToken cancellationToken) =>
        SendAsync(userId, (id, ct) => channel.RunStageAsync(id, command, ct), new AgentStageReply(false, AgentStageReply.Failed), cancellationToken);

    /// <inheritdoc />
    public async Task CancelStageAsync(Guid userId, Guid jobId) =>
        await SendAsync(
            userId,
            async (id, ct) =>
            {
                await channel.CancelStageAsync(id, jobId, ct).ConfigureAwait(false);
                return string.Empty;
            },
            string.Empty,
            CancellationToken.None).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<AgentStreamReply?> OpenStreamAsync(Guid userId, AgentStreamRequest request, CancellationToken cancellationToken) =>
        SendAsync(userId, (id, ct) => channel.OpenStreamAsync(id, request, ct), null, cancellationToken);

    /// <summary>Hatanın bağlantı kopması anlamına gelip gelmediği (iptal dışındaki gönderim hataları).</summary>
    /// <param name="exception">Hata.</param>
    /// <returns>Koptuysa <c>true</c>.</returns>
    protected abstract bool IsConnectionLost(Exception exception);

    private async Task<T?> SendAsync<T>(Guid userId, Func<string, CancellationToken, Task<T>> send, T? onError, CancellationToken cancellationToken)
        where T : class
    {
        if (registry.Find(userId) is not { } session)
        {
            return null;
        }

        try
        {
            return await send(session.ConnectionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && IsConnectionLost(ex))
        {
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return onError;
        }
    }
}
