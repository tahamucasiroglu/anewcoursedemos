using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Yönetim panelinin kullanıcı listesi / yasaklama köprüsü. Diagnostics kullanıcı tablosuna sahip değildir; API'de
/// <c>IdentityAdminUserDirectory</c> (Infrastructure, Identity'nin genel <c>IUserDirectory</c>'sini sarar) kayıtlıdır.
/// Kimlik modülü olmayan bir barındırmada <c>UnavailableAdminUserDirectory</c> kullanılabilir (<see cref="IsAvailable"/> = false, boş liste).
/// </summary>
public interface IAdminUserDirectory
{
    /// <summary>Gerçek bir kullanıcı kaynağı bağlı mı.</summary>
    bool IsAvailable { get; }

    /// <summary>Kullanıcıları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Kullanıcıyı yasaklar ya da yasağını kaldırır (yasaklı kullanıcının oturumları kapatılmalıdır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="banned">Yasaklı olsun mu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken cancellationToken);
}
