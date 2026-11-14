using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Oturumdaki ajanın artefakt indirme/onay uçları; sahiplik kontrolüyle <see cref="IArtifactStore"/>'a devreder.
/// </summary>
public sealed class AgentArtifactService : AgentsServiceBase, IAgentArtifactService
{
    private readonly IArtifactStore _store;
    private readonly ICurrentUser _currentUser;

    /// <summary>Oluşturur.</summary>
    /// <param name="store">Artefakt deposu.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="clock">Saat.</param>
    public AgentArtifactService(IArtifactStore store, ICurrentUser currentUser, IClock clock)
        : base(clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Artefaktı açar.</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İçerik.</returns>
    public async Task<Result<ArtifactContent>> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = RequireUser(_currentUser);
        return user.IsFailure ? user.Error : await _store.OpenAsync(user.Value, id, cancellationToken);
    }

    /// <summary>İndirmeyi onaylar.</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = RequireUser(_currentUser);
        return user.IsFailure ? user.Error : await _store.DeleteAsync(user.Value, id, cancellationToken);
    }
}
