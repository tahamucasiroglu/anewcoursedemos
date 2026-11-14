using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ajan token üreticisi (Agents → Identity köprüsü). Uygulama: <c>IdentityAgentTokenIssuer</c> (Infrastructure) Identity'nin
/// <c>ITokenIssuer</c>'ını <c>TokenClientKind.Agent</c> ile çağırır (ayrı refresh ailesi) ve iptal için <c>ITokenRevoker</c>'ı kullanır.
/// </summary>
public interface IAgentTokenIssuer
{
    /// <summary>Ajan için erişim + yenileme token'ı üretir.</summary>
    /// <param name="request">Kullanıcı, cihaz, bilgisayar adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti ve aile kimliği.</returns>
    Task<Result<AgentIssuedTokens>> IssueAsync(AgentTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cihazın refresh token ailesini iptal eder (eşleme kaldırılınca).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile (yoksa işlem yapılmaz).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RevokeAsync(Guid userId, Guid? tokenFamilyId, CancellationToken cancellationToken = default);
}
