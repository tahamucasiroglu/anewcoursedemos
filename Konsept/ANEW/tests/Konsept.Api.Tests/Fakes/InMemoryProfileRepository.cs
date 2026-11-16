using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte profil deposu: her işlem kullanıcıyla süzülür, <c>(user_id, name_key)</c> tekildir (gerçek deponun
/// sözleşmesi). Gerçek PostgreSQL davranışı ve sahiplik süzgecinin kasıtlı bozma kanıtı <c>ProfilesIntegrationTests</c>'te.
/// </summary>
public sealed class InMemoryProfileRepository : IProfileRepository
{
    private readonly Lock _gate = new();
    private readonly List<Profile> _rows = [];
    private readonly Dictionary<Guid, Guid> _selections = [];

    /// <summary>Gets tüm satırların anlık kopyası (testin "veritabanına" bakışı).</summary>
    public IReadOnlyList<Profile> Rows
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
    public Task<IReadOnlyList<Profile>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Profile>>([.. _rows.Where(r => r.UserId == userId)]);
        }
    }

    /// <inheritdoc />
    public Task<Profile?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.FirstOrDefault(r => r.UserId == userId && r.Id == profileId));
        }
    }

    /// <inheritdoc />
    public Task<bool> AddAsync(Profile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        lock (_gate)
        {
            if (_rows.Any(r => r.UserId == profile.UserId && r.NameKey == profile.NameKey))
            {
                return Task.FromResult(false);
            }

            _rows.Add(profile);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(Profile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        lock (_gate)
        {
            if (_rows.Any(r => r.UserId == profile.UserId && r.Id != profile.Id && r.NameKey == profile.NameKey))
            {
                return Task.FromResult(false);
            }

            var index = _rows.FindIndex(r => r.UserId == profile.UserId && r.Id == profile.Id);
            if (index >= 0)
            {
                _rows[index] = profile;
            }

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.RemoveAll(r => r.UserId == userId && r.Id == profileId) > 0);
        }
    }

    /// <inheritdoc />
    public Task<Guid?> GetSelectionAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_selections.TryGetValue(userId, out var id) ? (Guid?)id : null);
        }
    }

    /// <inheritdoc />
    public Task SetSelectionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _selections[userId] = profileId;
        }

        return Task.CompletedTask;
    }
}
