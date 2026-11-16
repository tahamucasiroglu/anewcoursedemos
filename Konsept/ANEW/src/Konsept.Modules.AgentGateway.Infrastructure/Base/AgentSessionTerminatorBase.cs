using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Cihazı iptal edilen ajanın canlı bağlantısını kapatır (spec 0012 Req 6, 12; BR-14): ajana önce "oturum kapandı" bildirilir,
/// kısa bir süre sonra bağlantı sunucu tarafında kesilir. Bildirim ulaşmasa da ajan eski belirteçle yeniden bağlanamaz (401).
/// Bildirim yolu alt sınıftadır.
/// </summary>
/// <param name="tracker">Açık bağlantılar.</param>
public abstract class AgentSessionTerminatorBase(IAgentConnectionTracker tracker) : IAgentDeviceRevocationListener
{
    /// <summary>Bildirim ile kesme arasındaki bekleme (bildirim ajana ulaşsın diye).</summary>
    public static readonly TimeSpan NoticeGrace = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc />
    public async Task OnDevicesRevokedAsync(Guid userId, IReadOnlyCollection<Guid> deviceIds, CancellationToken cancellationToken)
    {
        var connections = tracker.Take(userId, deviceIds);
        if (connections.Count == 0)
        {
            return;
        }

        foreach (var connection in connections)
        {
            await NotifyClosedAsync(connection.Key, cancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(NoticeGrace, CancellationToken.None).ConfigureAwait(false);
        foreach (var connection in connections)
        {
            connection.Value();
        }
    }

    /// <summary>Ajana "oturum kapandı" bildirir (en iyi çaba).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task NotifyClosedAsync(string connectionId, CancellationToken cancellationToken);
}
