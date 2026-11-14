using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Infrastructure.Security.Base;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Agents.Infrastructure.Security.Concrete;

/// <summary>
/// Identity köprüsü: token'ı Identity'nin <see cref="ITokenIssuer"/>'ı ile <see cref="TokenClientKind.Agent"/> türünde (yeni refresh ailesi)
/// üretir; eşleme kaldırılınca <see cref="ITokenRevoker.RevokeFamilyAsync"/> ile aileyi iptal eder.
/// Not: Identity token'ı <c>agent_device_id</c> claim'i taşımaz; hub ajanı Hello'daki bilgisayar adıyla eşler.
/// </summary>
public sealed class IdentityAgentTokenIssuer : AgentTokenIssuerBase
{
    private readonly ITokenIssuer _issuer;
    private readonly ITokenRevoker _revoker;

    /// <summary>Oluşturur.</summary>
    /// <param name="issuer">Identity token üretici.</param>
    /// <param name="revoker">Identity token iptalcisi.</param>
    public IdentityAgentTokenIssuer(ITokenIssuer issuer, ITokenRevoker revoker)
    {
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _revoker = revoker ?? throw new ArgumentNullException(nameof(revoker));
    }

    /// <summary>Identity ile token üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    protected override async Task<Result<AgentIssuedTokens>> IssueCoreAsync(AgentTokenRequest request, CancellationToken cancellationToken)
    {
        var issued = await _issuer.IssueAsync(request.UserId, TokenClientKind.Agent, cancellationToken);
        return issued.Map(t => new AgentIssuedTokens(t.Tokens, t.FamilyId));
    }

    /// <summary>Aileyi Identity'de iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task RevokeCoreAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken) =>
        await _revoker.RevokeFamilyAsync(userId, tokenFamilyId, cancellationToken);
}
