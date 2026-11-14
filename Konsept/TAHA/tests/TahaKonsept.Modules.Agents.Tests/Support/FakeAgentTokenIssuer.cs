using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Sahte ajan token üretici: her çağrıda yeni aile ile sahte token döner; üretilen ve iptal edilen aileleri kaydeder.
/// </summary>
public sealed class FakeAgentTokenIssuer : IAgentTokenIssuer
{
    /// <summary>Üretim istekleri.</summary>
    public List<AgentTokenRequest> Issued { get; } = [];

    /// <summary>İptal edilen aileler.</summary>
    public List<Guid> RevokedFamilies { get; } = [];

    /// <summary>Token üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    public Task<Result<AgentIssuedTokens>> IssueAsync(AgentTokenRequest request, CancellationToken cancellationToken = default)
    {
        Issued.Add(request);
        var tokens = new TokenResponse { AccessToken = "access-" + Issued.Count, RefreshToken = "refresh-" + Issued.Count };
        return Task.FromResult(Result.Success(new AgentIssuedTokens(tokens, Guid.NewGuid())));
    }

    /// <summary>Aileyi iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task RevokeAsync(Guid userId, Guid? tokenFamilyId, CancellationToken cancellationToken = default)
    {
        if (tokenFamilyId is { } family)
        {
            RevokedFamilies.Add(family);
        }

        return Task.CompletedTask;
    }
}
