using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Domain.Rules;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// Ajan varlığının ortak mantığı: oturumun sahibiyle kaydı ve kullanıcıya göre durum okuma (spec 0012 Req 13); olay yazımını
/// alt sınıfa bırakır.
/// </summary>
public abstract class AgentPresenceServiceBase : IAgentPresenceService
{
    /// <summary>Initializes a new instance of the <see cref="AgentPresenceServiceBase"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    protected AgentPresenceServiceBase(IAgentConnectionRegistry registry, TimeProvider timeProvider)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets ajan oturum kaydı.</summary>
    protected IAgentConnectionRegistry Registry { get; }

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>Gets kurulu bileşen deposu (spec 0006); verilmemişse saklanmaz.</summary>
    protected IInstalledComponentStore? Components { get; init; }

    /// <summary>Gets sürüm kapısı (spec 0023); verilmemişse her sürüm kabul edilir.</summary>
    protected AgentVersionGate? Gate { get; init; }

    /// <summary>Gets kullanıcının "Üretiliyor" işi var mı okuyucusu (spec 0023 Req 3); verilmemişse yok sayılır.</summary>
    protected IProductionActivityReader? Activity { get; init; }

    /// <inheritdoc />
    public AgentStatusDto GetStatus(Guid? userId)
    {
        var current = userId is { } id ? Registry.Find(id) : null;
        return current is null
            ? new AgentStatusDto(false, null, null, null)
            : new AgentStatusDto(!current.UpdateRequired, current.MachineName, current.AgentVersion, current.ConnectedAt, current.UpdateRequired);
    }

    /// <inheritdoc />
    public async Task<AgentRegisterReply> RegisterAsync(string connectionId, AgentOwner owner, AgentRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(registration);
        var updateRequired = Gate?.RequiresUpdate(registration.AgentVersion) == true;
        var session = new AgentSession(
            connectionId, registration.MachineName, registration.AgentVersion, TimeProvider.GetUtcNow(), owner.UserId, owner.DeviceId, updateRequired);
        Registry.Set(session);
        Components?.Set(owner.UserId, registration.InstalledComponents ?? []); // Kurulu bileşenler (spec 0006 Req 19).
        await RunSideEffectAsync(() => OnConnectedAsync(session, cancellationToken), cancellationToken).ConfigureAwait(false);
        return new AgentRegisterReply(Gate?.ServerVersion ?? registration.AgentVersion, updateRequired);
    }

    /// <inheritdoc />
    public async Task<bool> BeginUpdateAsync(string connectionId, AgentOwner owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (Registry.FindByConnection(connectionId) is not { } session || session.UserId != owner.UserId)
        {
            return false;
        }

        if (await IsProducingAsync(owner.UserId, cancellationToken).ConfigureAwait(false))
        {
            return false; // "Üretim sürüyor; iş bitince ya da durdurulunca güncellenecek" (Req 3): ajan bir süre sonra yeniden sorar.
        }

        Registry.Set(session with { Updating = true });
        return true;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken)
    {
        var removed = Registry.Remove(connectionId);
        if (removed is not null)
        {
            await RunSideEffectAsync(() => OnDisconnectedAsync(removed, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Olay yazımı gibi bir yan etki başarısız olduğunda çağrılır. Varlık durumu yan etkiye bağlı değildir:
    /// veritabanı kapalı olsa da ajan kaydı başarılı döner (review round 1, M-1).
    /// </summary>
    /// <param name="exception">Oluşan hata.</param>
    protected abstract void OnSideEffectFailed(Exception exception);

    /// <summary>Ajan bağlandığında çağrılır.</summary>
    /// <param name="session">Yeni oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task OnConnectedAsync(AgentSession session, CancellationToken cancellationToken);

    /// <summary>Geçerli ajanın bağlantısı koptuğunda çağrılır.</summary>
    /// <param name="session">Kapanan oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task OnDisconnectedAsync(AgentSession session, CancellationToken cancellationToken);

    // Üretim durumu okunamazsa (veritabanı yok) iş de yürüyemez: güncelleme engellenmez, hata loglanır.
    private async Task<bool> IsProducingAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (Activity is null)
        {
            return false;
        }

        try
        {
            return await Activity.IsProducingAsync(userId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OnSideEffectFailed(ex);
            return false;
        }
    }

    private async Task RunSideEffectAsync(Func<Task> sideEffect, CancellationToken cancellationToken)
    {
        try
        {
            await sideEffect().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OnSideEffectFailed(ex);
        }
    }
}
