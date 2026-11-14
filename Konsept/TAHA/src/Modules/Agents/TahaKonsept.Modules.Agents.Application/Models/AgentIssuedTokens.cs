using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Agents.Application.Models;

/// <summary>
/// Ajan için üretilen token çifti ve refresh token ailesi (cihaz kaydında saklanır; eşleme kaldırılınca aile iptal edilir).
/// </summary>
/// <param name="Tokens">Erişim + yenileme token'ı.</param>
/// <param name="TokenFamilyId">Refresh token ailesi (bilinmiyorsa <c>null</c>).</param>
public sealed record AgentIssuedTokens(TokenResponse Tokens, Guid? TokenFamilyId);
