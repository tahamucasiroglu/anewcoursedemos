using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// "Ajanı test et" akışının ortak mantığı: bağlı ajan kontrolü ve 10 sn zaman aşımı.
/// </summary>
public abstract class AgentTestServiceBase : IAgentTestService
{
    /// <summary>Ajandan cevap için beklenen en uzun süre (spec 0001 AC-7).</summary>
    public static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Initializes a new instance of the <see cref="AgentTestServiceBase"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı (zaman aşımı bununla ölçülür).</param>
    protected AgentTestServiceBase(IAgentConnectionRegistry registry, TimeProvider timeProvider)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets ajan oturum kaydı.</summary>
    protected IAgentConnectionRegistry Registry { get; }

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<Result<AgentTestResultDto>> TestAsync(CancellationToken cancellationToken)
    {
        // Kullanıcı yalnızca kendi ajanına ulaşır (spec 0012 Req 13): başkasının ajanı "bağlı değil" görünür.
        var session = RequestingUserId is { } userId ? Registry.Find(userId) : null;
        if (session is null)
        {
            return Result<AgentTestResultDto>.Failure(AgentErrors.NotConnected);
        }

        using var timeout = new CancellationTokenSource(ReplyTimeout, TimeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            var reply = await SendPingAsync(session.ConnectionId, linked.Token).ConfigureAwait(false);
            return Result<AgentTestResultDto>.Success(new AgentTestResultDto(reply.MachineName, reply.AgentVersion));
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return Result<AgentTestResultDto>.Failure(AgentErrors.Timeout);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && IsConnectionLost(ex))
        {
            return Result<AgentTestResultDto>.Failure(AgentErrors.NotConnected);
        }
    }

    /// <summary>Gets isteği yapan kullanıcı; giriş yapılmamışsa <c>null</c>.</summary>
    protected abstract Guid? RequestingUserId { get; }

    /// <summary>Komutu ajana gönderir.</summary>
    /// <param name="connectionId">Ajan bağlantısı.</param>
    /// <param name="cancellationToken">Zaman aşımını da taşıyan iptal belirteci.</param>
    /// <returns>Ajanın cevabı.</returns>
    protected abstract Task<AgentPingReply> SendPingAsync(string connectionId, CancellationToken cancellationToken);

    /// <summary>Hatanın "bağlantı yok" anlamına gelip gelmediğini söyler.</summary>
    /// <param name="exception">Gönderim sırasında oluşan hata.</param>
    /// <returns>Bağlantı kopmuşsa <c>true</c>.</returns>
    protected abstract bool IsConnectionLost(Exception exception);
}
