using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;

namespace TahaKonsept.Modules.Agents.Infrastructure.Security.Concrete;

/// <summary>
/// Identity'nin oturum iptali bildirimlerini ajanlara uygular: logout-all/yasaklamada kullanıcının tüm ajanları iptal edilip hub'dan düşürülür;
/// tek aile iptalinde o aileye bağlı cihaz iptal edilir. Kayıt: <c>TryAddEnumerable(Scoped&lt;ISessionRevocationNotifier, AgentSessionRevocationNotifier&gt;)</c>.
/// </summary>
public sealed class AgentSessionRevocationNotifier : SessionRevocationNotifierBase
{
    private readonly IAgentSessionRevoker _revoker;

    /// <summary>Oluşturur.</summary>
    /// <param name="revoker">Ajan oturum iptalcisi.</param>
    public AgentSessionRevocationNotifier(IAgentSessionRevoker revoker)
    {
        _revoker = revoker ?? throw new ArgumentNullException(nameof(revoker));
    }

    /// <summary>Tüm oturumlar iptal edildi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken ct = default) => _revoker.RevokeUserAsync(userId, ct);

    /// <summary>Tek aile iptal edildi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task OnTokenFamilyRevokedAsync(Guid userId, Guid familyId, CancellationToken ct = default) =>
        _revoker.RevokeTokenFamilyAsync(userId, familyId, ct);
}
