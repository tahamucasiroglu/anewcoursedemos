using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Admin Kullanıcılar sekmesi (spec 0024): liste, ban ve ban kaldırma. Yalnızca <c>admin.panel</c> korumalı uçlardan çağrılır
/// (yetki kontrolü uçtadır); admin hesapları sunucuda da banlanamaz.
/// </summary>
public interface IAdminUserService
{
    /// <summary>Kullanıcıları 25'erli sayfa, arama ve durum filtresiyle listeler.</summary>
    /// <param name="request">Sayfa, arama, durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<AdminUserPageDto>> ListAsync(AdminUsersRequest? request, CancellationToken cancellationToken);

    /// <summary>Kullanıcıyı banlar: oturumlar, ajan ve arka plan işleri anında düşer (BR-13); zaten banlıysa idempotent.</summary>
    /// <param name="userId">Hedef kullanıcı.</param>
    /// <param name="request">Sebep.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel satır.</returns>
    Task<Result<AdminUserDto>> BanAsync(Guid userId, BanUserRequest? request, CancellationToken cancellationToken);

    /// <summary>Banı kaldırır (eski oturumlar geri gelmez); banlı değilse idempotent.</summary>
    /// <param name="userId">Hedef kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel satır.</returns>
    Task<Result<AdminUserDto>> UnbanAsync(Guid userId, CancellationToken cancellationToken);
}
