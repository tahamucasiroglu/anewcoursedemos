using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Yönetim paneli kullanıcı ekranı: liste, yasakla, yasağı kaldır (yönetici). Veri <see cref="IAdminUserDirectory"/>'den gelir.
/// </summary>
public interface IAdminUserService : IApplicationService
{
    /// <summary>Kullanıcıları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<Result<IReadOnlyList<AdminUserDto>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıyı yasaklar (kendini yasaklayamaz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> BanAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Yasağı kaldırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> UnbanAsync(Guid userId, CancellationToken cancellationToken = default);
}
