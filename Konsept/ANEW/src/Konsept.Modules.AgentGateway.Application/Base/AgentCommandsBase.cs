using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;

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
    public const string AgentProtocol = AgentToolCheckReply.AgentProtocol;

    /// <inheritdoc />
    public Guid? ConnectedDevice(Guid userId) => Commandable(userId)?.DeviceId;

    /// <inheritdoc />
    public bool UpdateRequired(Guid userId) => registry.Find(userId)?.UpdateRequired == true;

    /// <inheritdoc />
    public Task<AgentToolCheckReply?> CheckToolsAsync(Guid userId, AgentToolCheckRequest request, CancellationToken cancellationToken) =>
        SendAsync(userId, (id, ct) => channel.CheckToolsAsync(id, request, ct), new AgentToolCheckReply([AgentProtocol]), cancellationToken);

    /// <inheritdoc />
    public Task<AgentInstallReply?> InstallComponentsAsync(Guid userId, AgentInstallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync(userId, (id, ct) => channel.InstallComponentsAsync(id, request, ct), new AgentInstallReply(false, request.Components), cancellationToken);
    }

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

    /// <inheritdoc />
    public Task<AgentFileActionReply?> OpenFileAsync(Guid userId, AgentFileActionRequest request, CancellationToken cancellationToken) =>
        SendAsync(userId, (id, ct) => channel.OpenFileAsync(id, request, ct), new AgentFileActionReply(false, AgentFileActionReply.Failed), cancellationToken);

    /// <inheritdoc />
    public Task<AgentDeleteFilesReply?> DeleteFilesAsync(Guid userId, AgentDeleteFilesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync(userId, (id, ct) => channel.DeleteFilesAsync(id, request, ct), new AgentDeleteFilesReply(request.FileNames), cancellationToken);
    }

    /// <summary>Hatanın bağlantı kopması anlamına gelip gelmediği (iptal dışındaki gönderim hataları).</summary>
    /// <param name="exception">Hata.</param>
    /// <returns>Koptuysa <c>true</c>.</returns>
    protected abstract bool IsConnectionLost(Exception exception);

    // Sürümü uyumsuz ya da güncellemedeki ajan üretim açısından bağlı değildir (spec 0023 Req 3, 15): ona hiçbir komut gitmez.
    private AgentSession? Commandable(Guid userId) => registry.Find(userId) is { AcceptsCommands: true } session ? session : null;

    private async Task<T?> SendAsync<T>(Guid userId, Func<string, CancellationToken, Task<T>> send, T? onError, CancellationToken cancellationToken)
        where T : class
    {
        if (Commandable(userId) is not { } session)
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
