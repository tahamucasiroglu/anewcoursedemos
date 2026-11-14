using TahaKonsept.Modules.Identity.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="ISessionRevocationNotifier"/> tabanı: tüm olaylar varsayılan olarak hiçbir şey yapmaz.
/// Başka modüller (ör. Agents) yalnızca ilgilendikleri olayı override eder.
/// </summary>
public abstract class SessionRevocationNotifierBase : ISessionRevocationNotifier
{
    /// <summary>Tüm oturumlar iptal edildi (varsayılan: hiçbir şey).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>Tek aile iptal edildi (varsayılan: hiçbir şey).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task OnTokenFamilyRevokedAsync(Guid userId, Guid familyId, CancellationToken ct = default) => Task.CompletedTask;
}
