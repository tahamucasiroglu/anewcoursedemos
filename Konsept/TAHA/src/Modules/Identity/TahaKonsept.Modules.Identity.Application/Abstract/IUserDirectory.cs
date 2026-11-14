using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// PUBLIC modüller arası servis (Diagnostics / admin paneli): kullanıcı listesi, sayısı, yasaklama.
/// Yasaklanan kullanıcının tüm token'ları iptal edilir; son etkin yönetici yasaklanamaz (kilitlenmeyi önler).
/// </summary>
public interface IUserDirectory
{
    /// <summary>Tüm kullanıcıları listeler (ada göre).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kullanıcı özetleri.</returns>
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Toplam kullanıcı sayısı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Kullanıcıyı yasaklar ya da yasağı kaldırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="banned">true: yasakla, false: yasağı kaldır.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç (NotFound / Conflict olabilir).</returns>
    Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken ct = default);
}
