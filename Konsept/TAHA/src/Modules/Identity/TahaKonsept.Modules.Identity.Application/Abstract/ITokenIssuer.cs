using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// PUBLIC modüller arası servis: bir kullanıcı için yeni token ailesiyle erişim + refresh token çifti verir.
/// Agents modülü cihaz eşlemede (<c>api/agents/device/token</c>) <see cref="TokenClientKind.Agent"/> ile çağırır.
/// Yasaklı ya da bulunamayan kullanıcı için hata döner.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>Yeni aile ile token çifti üretir ve kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="kind">İstemci türü (web / ajan).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token çifti + aile kimliği (ajanı kaldırırken <see cref="ITokenRevoker.RevokeFamilyAsync"/> için saklanır).</returns>
    Task<Result<IssuedTokens>> IssueAsync(Guid userId, TokenClientKind kind, CancellationToken ct = default);
}
