using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Models;

/// <summary>
/// <see cref="Abstract.ITokenIssuer.IssueAsync"/> sonucu: istemciye gidecek token çifti ve yeni ailenin kimliği
/// (ajan kaydında saklanıp ajan kaldırılınca aile iptali için kullanılır).
/// </summary>
/// <param name="Tokens">Erişim + refresh token çifti.</param>
/// <param name="FamilyId">Refresh token ailesi.</param>
public sealed record IssuedTokens(TokenResponse Tokens, Guid FamilyId);
