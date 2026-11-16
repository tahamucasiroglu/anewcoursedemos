using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Concepts.Contracts.Enums;

namespace Konsept.Modules.Concepts.Contracts.Abstract;

/// <summary>
/// Üretim sırasının (Production, spec 0006) konsept durumlarını değiştirdiği tek yol (mimari iletişim kuralı 1): okuma ve koşullu durum
/// geçişi. Her işlem kullanıcı kimliğiyle süzülür (BR-1); silinmiş konsept bulunmaz.
/// </summary>
public interface IConceptQueueBridge
{
    /// <summary>Kullanıcının silinmemiş konseptleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ids">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bulunanlar (başkasınınki ve silinmiş yok).</returns>
    Task<IReadOnlyList<QueueableConcept>> FindAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Durumu yalnızca konsept <paramref name="from"/> durumundaysa değiştirir (tek koşullu ifade).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="from">Beklenen durum.</param>
    /// <param name="to">Yeni durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değiştiyse <c>true</c>.</returns>
    Task<bool> ChangeAsync(Guid userId, Guid conceptId, ConceptQueueStage from, ConceptQueueStage to, CancellationToken cancellationToken);

    /// <summary>Kullanıcının silinmemiş konseptinin metni (spec 0017).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Metin ya da <c>null</c> (yok, başkasının ya da silinmiş).</returns>
    Task<ConceptContent?> GetContentAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
