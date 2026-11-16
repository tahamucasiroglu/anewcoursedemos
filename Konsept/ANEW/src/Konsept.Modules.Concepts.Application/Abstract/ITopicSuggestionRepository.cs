using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Konu önerilerinin kalıcılığı; her işlem kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface ITopicSuggestionRepository
{
    /// <summary>Profilin tüm önerileri (her durumda), en eski önce.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öneriler.</returns>
    Task<IReadOnlyList<TopicSuggestion>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Öneri ekler.</summary>
    /// <param name="suggestions">Öneriler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddRangeAsync(IReadOnlyCollection<TopicSuggestion> suggestions, CancellationToken cancellationToken);

    /// <summary>Kullanıcının profilindeki bekleyen önerilerin durumunu değiştirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="ids">Öneriler; <c>null</c> → profilin bekleyen tüm önerileri.</param>
    /// <param name="state">Yeni durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SetPendingStateAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid>? ids, SuggestionState state, CancellationToken cancellationToken);

    /// <summary>Seçilip konsepte çevrilemeyen (durdurulan ya da hata veren) önerileri yeniden bekleyen yapar (review r1 M-4).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="ids">Öneriler (yalnızca "seçildi" durumundakiler değişir).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RestoreSelectedAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
