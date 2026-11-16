using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte API key deposu: kullanıcı + sağlayıcı tekil, her işlem kullanıcıyla süzülür (gerçek deponun
/// sözleşmesi). Gerçek PostgreSQL davranışı ve sahiplik süzgecinin kasıtlı bozma kanıtı <c>AiGatewayIntegrationTests</c>'te.
/// </summary>
public sealed class InMemoryApiKeyRepository : IApiKeyRepository
{
    private readonly Lock _gate = new();
    private readonly List<UserApiKey> _rows = [];

    /// <summary>Gets tüm satırların anlık kopyası (testin "veritabanına" bakışı).</summary>
    public IReadOnlyList<UserApiKey> Rows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows];
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UserApiKey>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<UserApiKey>>([.. _rows.Where(r => r.UserId == userId)]);
        }
    }

    /// <inheritdoc />
    public Task UpsertAsync(UserApiKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_gate)
        {
            _rows.RemoveAll(r => r.UserId == key.UserId && r.ProviderId == key.ProviderId);
            _rows.Add(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid userId, string providerId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.RemoveAll(r => r.UserId == userId && r.ProviderId == providerId) > 0);
        }
    }
}
