using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte konsept ayarı deposu: profil başına tek satır, okuma kullanıcıyla süzülür, başka kullanıcının
/// satırının üzerine yazılmaz (gerçek deponun sözleşmesi). Gerçek PostgreSQL davranışı <c>ProfilesIntegrationTests</c>'te.
/// </summary>
public sealed class InMemoryConceptSettingsRepository : IConceptSettingsRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, ConceptSettings> _rows = [];

    /// <summary>Gets tüm satırların anlık kopyası (testin "veritabanına" bakışı).</summary>
    public IReadOnlyList<ConceptSettings> Rows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows.Values];
            }
        }
    }

    /// <inheritdoc />
    public Task<ConceptSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.TryGetValue(profileId, out var row) && row.UserId == userId ? row : null);
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(ConceptSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
        {
            if (!_rows.TryGetValue(settings.ProfileId, out var row) || row.UserId == settings.UserId)
            {
                _rows[settings.ProfileId] = settings;
            }
        }

        return Task.CompletedTask;
    }
}
