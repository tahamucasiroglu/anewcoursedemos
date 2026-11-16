using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Cihaz iptali (spec 0012 Req 6, 12): kullanıcının etkin cihazları tek işlemle iptal edilir, sonra her dinleyiciye bildirilir.
/// Dinleyici hatası iptali geri almaz (cihaz zaten geçersiz; eski belirteçle yeniden bağlanılamaz), alt sınıf loglar.
/// </summary>
/// <param name="devices">Cihaz deposu.</param>
/// <param name="listeners">İptal dinleyicileri (AgentGateway).</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class AgentDeviceRevokerBase(
    IAgentDeviceRepository devices,
    IEnumerable<IAgentDeviceRevocationListener> listeners,
    TimeProvider timeProvider) : IAgentDeviceRevoker
{
    /// <inheritdoc />
    public async Task<int> RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var revoked = await devices.RevokeActiveAsync(userId, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (revoked.Count == 0)
        {
            return 0;
        }

        foreach (var listener in listeners)
        {
            try
            {
                await listener.OnDevicesRevokedAsync(userId, revoked, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                OnListenerFailed(ex);
            }
        }

        return revoked.Count;
    }

    /// <summary>Bir dinleyici hata verdiğinde çağrılır.</summary>
    /// <param name="exception">Hata.</param>
    protected abstract void OnListenerFailed(Exception exception);
}
