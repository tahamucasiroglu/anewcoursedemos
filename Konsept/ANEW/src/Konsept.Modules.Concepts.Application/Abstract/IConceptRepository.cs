using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Konseptlerin kalıcılığı; her işlem kullanıcı kimliğiyle süzülür (BR-1): başka kullanıcının konsepti okunmaz, değişmez.</summary>
public interface IConceptRepository
{
    /// <summary>Konsept ekler.</summary>
    /// <param name="concept">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(Concept concept, CancellationToken cancellationToken);

    /// <summary>Profilin silinmemiş konseptleri, durum filtresiyle, en yeni üstte, sayfalı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="statuses">Durumlar.</param>
    /// <param name="skip">Atlanan.</param>
    /// <param name="take">Alınan.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa ve toplam.</returns>
    Task<ConceptPage> ListAsync(Guid userId, Guid profileId, IReadOnlyCollection<ConceptStatus> statuses, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Kullanıcının silinmemiş konseptleri (kimliklerle).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ids">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bulunanlar (başkasınınkiler yok).</returns>
    Task<IReadOnlyList<Concept>> GetManyAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Durum değiştirir; yalnızca kullanıcının, <paramref name="from"/> durumundaki konseptleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ids">Kimlikler.</param>
    /// <param name="from">Beklenen durum.</param>
    /// <param name="to">Yeni durum.</param>
    /// <param name="at">Zaman (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değişen sayı.</returns>
    Task<int> ChangeStatusAsync(Guid userId, IReadOnlyCollection<Guid> ids, ConceptStatus from, ConceptStatus to, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>
    /// Sahneleri ve uyarıları değiştirir (spec 0015); yalnızca kullanıcının, silinmemiş ve <paramref name="allowed"/> durumundaki konsepti.
    /// Durum ve başlık değişmez.
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="allowed">İzin verilen durumlar.</param>
    /// <param name="scenes">Yeni sahneler.</param>
    /// <param name="wordRepetition">Kelime tekrarı uyarısı.</param>
    /// <param name="durationMismatch">Süre uyumsuz uyarısı.</param>
    /// <param name="at">Zaman (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değiştiyse <c>true</c>.</returns>
    Task<bool> UpdateScenesAsync(
        Guid userId,
        Guid conceptId,
        IReadOnlyCollection<ConceptStatus> allowed,
        IReadOnlyList<ConceptScene> scenes,
        bool wordRepetition,
        bool durationMismatch,
        DateTimeOffset at,
        CancellationToken cancellationToken);

    /// <summary>Profilin tüm konsept başlıkları (durum ve silinme işaretinden bağımsız; BR-4 geçmişi).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başlıklar (konudan üretilenler "seçilen").</returns>
    Task<IReadOnlyList<HistoryTitle>> ListTitlesAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının en az bir konsepti olan profilleri (silinmişler dahil).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil kimlikleri.</returns>
    Task<IReadOnlySet<Guid>> ProfileIdsAsync(Guid userId, CancellationToken cancellationToken);
}
