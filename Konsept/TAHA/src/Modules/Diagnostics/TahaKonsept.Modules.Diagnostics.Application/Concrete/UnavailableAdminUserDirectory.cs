using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Domain.Errors;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// Kimlik modülü olmayan barındırmalar/testler için yer tutucu <c>IAdminUserDirectory</c>: boş liste, yasaklama "kullanılamıyor".
/// API'de bunun yerine <c>IdentityAdminUserDirectory</c> kayıtlıdır.
/// </summary>
public sealed class UnavailableAdminUserDirectory : AdminUserDirectoryBase
{
    /// <summary>Gerçek kaynak bağlı değil.</summary>
    public override bool IsAvailable => false;

    /// <summary>Boş liste.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Boş liste.</returns>
    public override Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminUserDto>>([]);

    /// <summary>Her zaman "kullanılamıyor".</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="banned">Yasaklı mı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başarısız sonuç.</returns>
    public override Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure(DiagnosticsErrors.UserDirectoryUnavailable));
}
