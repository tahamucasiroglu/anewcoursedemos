using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Base;

/// <summary>
/// <see cref="IAdminUserDirectory"/> tabanı. Varsayılan <see cref="IsAvailable"/> = <c>true</c>; adaptörler (ör. Identity'yi saran sınıf)
/// <see cref="ListAsync"/> ve <see cref="SetBannedAsync"/> yazar.
/// </summary>
public abstract class AdminUserDirectoryBase : IAdminUserDirectory
{
    /// <summary>Gerçek kaynak bağlı mı.</summary>
    public virtual bool IsAvailable => true;

    /// <summary>Kullanıcıları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public abstract Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Yasak durumunu değiştirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="banned">Yasaklı mı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public abstract Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken cancellationToken);
}
