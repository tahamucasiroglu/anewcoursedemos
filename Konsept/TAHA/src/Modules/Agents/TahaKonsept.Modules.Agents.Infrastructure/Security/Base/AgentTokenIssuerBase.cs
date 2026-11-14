using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Infrastructure.Security.Base;

/// <summary>
/// Ajan token üretici tabanı: istek doğrulaması (<see cref="IssueAsync"/> boş kullanıcı/cihazı reddeder) ve
/// aile yoksa iptali atlama (<see cref="RevokeAsync"/>); asıl işler <see cref="IssueCoreAsync"/> / <see cref="RevokeCoreAsync"/>.
/// </summary>
public abstract class AgentTokenIssuerBase : IAgentTokenIssuer
{
    /// <summary>Token üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    public Task<Result<AgentIssuedTokens>> IssueAsync(AgentTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.UserId == Guid.Empty || request.AgentDeviceId == Guid.Empty)
        {
            throw new ArgumentException("Kullanıcı ve cihaz kimliği gerekli.", nameof(request));
        }

        return IssueCoreAsync(request, cancellationToken);
    }

    /// <summary>Aileyi iptal eder (aile yoksa hiçbir şey yapmaz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task RevokeAsync(Guid userId, Guid? tokenFamilyId, CancellationToken cancellationToken = default) =>
        tokenFamilyId is { } family && family != Guid.Empty
            ? RevokeCoreAsync(userId, family, cancellationToken)
            : Task.CompletedTask;

    /// <summary>Token üretimi.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    protected abstract Task<Result<AgentIssuedTokens>> IssueCoreAsync(AgentTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Aile iptali.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task RevokeCoreAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken);
}
