using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// PUBLIC modüller arası servis: refresh token iptali. <see cref="RevokeFamilyAsync"/> tek bir aileyi (ör. kaldırılan ajan),
/// <see cref="RevokeAllAsync"/> kullanıcının tüm ailelerini (web + ajan) iptal eder ve <see cref="ISessionRevocationNotifier"/>'ları çağırır.
/// </summary>
public interface ITokenRevoker
{
    /// <summary>Kullanıcının belirtilen token ailesini iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İptal edilen etkin token sayısı.</returns>
    Task<Result<int>> RevokeFamilyAsync(Guid userId, Guid familyId, CancellationToken ct = default);

    /// <summary>Kullanıcının tüm etkin refresh token'larını (ajan dahil) iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İptal edilen etkin token sayısı.</returns>
    Task<Result<int>> RevokeAllAsync(Guid userId, CancellationToken ct = default);
}
