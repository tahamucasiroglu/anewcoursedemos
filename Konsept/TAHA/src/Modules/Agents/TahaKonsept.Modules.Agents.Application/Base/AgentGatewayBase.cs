using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Ajan kapısı tabanı: kullanıcının birincil bağlantısını bulma (<see cref="ResolveConnection"/>), bağlantıya güvenli çağrı
/// (<c>InvokeAsync</c>: hata → <c>agents.send_failed</c>) ve yerel sunucu adres bilgisi (<see cref="ResolveAddress"/>).
/// </summary>
public abstract class AgentGatewayBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="tracker">Bağlantı izleyici.</param>
    /// <param name="clients">İstemci vekil sağlayıcı.</param>
    protected AgentGatewayBase(IAgentConnectionTracker tracker, IAgentClientProvider clients)
    {
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Clients = clients ?? throw new ArgumentNullException(nameof(clients));
    }

    /// <summary>Bağlantı izleyici.</summary>
    protected IAgentConnectionTracker Tracker { get; }

    /// <summary>İstemci vekil sağlayıcı.</summary>
    protected IAgentClientProvider Clients { get; }

    /// <summary>Kullanıcının birincil bağlantısı ya da çevrimdışı hatası.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bağlantı.</returns>
    protected Result<AgentConnection> ResolveConnection(Guid userId) =>
        Tracker.GetPrimary(userId) is { } connection ? connection : AgentErrors.AgentOffline;

    /// <summary>Kullanıcının birincil bağlantısına çağrı yapar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="call">Çağrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected async Task<Result> InvokeAsync(Guid userId, Func<IAgentClient, Task> call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        var connection = ResolveConnection(userId);
        if (connection.IsFailure)
        {
            return connection.Error;
        }

        return await InvokeAsync(connection.Value, call, cancellationToken);
    }

    /// <summary>Belirli bağlantıya çağrı yapar.</summary>
    /// <param name="connection">Bağlantı.</param>
    /// <param name="call">Çağrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected async Task<Result> InvokeAsync(AgentConnection connection, Func<IAgentClient, Task> call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(call);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await call(Clients.GetClient(connection.ConnectionId));
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentErrors.SendFailed with { Message = AgentErrors.SendFailed.Message + " (" + ex.Message + ")" };
        }
    }

    /// <summary>Birincil bağlantının yerel sunucu adres bilgisi (LAN, port, akış anahtarı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Hello almış bağlantı ya da hata.</returns>
    protected Result<AgentConnection> ResolveAddress(Guid userId)
    {
        var connection = ResolveConnection(userId);
        if (connection.IsFailure)
        {
            return connection;
        }

        var c = connection.Value;
        return string.IsNullOrWhiteSpace(c.LanAddress) || c.VideoServerPort is null or <= 0 || string.IsNullOrEmpty(c.StreamKey)
            ? AgentErrors.AgentAddressUnknown
            : connection;
    }
}
