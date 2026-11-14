using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Düzeltme adayı deposu (singleton, bellekte, süreli): ekle, bul, sil, konsepte ait hepsini sil.
/// </summary>
public interface IRevisionCandidateStore
{
    /// <summary>Aday ekler (süresi dolanları temizler).</summary>
    /// <param name="candidate">Aday.</param>
    void Add(RevisionCandidate candidate);

    /// <summary>Adayı bulur (kullanıcı + konsept eşleşmeli, süresi dolmamış).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <returns>Aday ya da null.</returns>
    RevisionCandidate? Find(Guid userId, Guid conceptId, Guid candidateId);

    /// <summary>Adayı siler.</summary>
    /// <param name="candidateId">Aday.</param>
    /// <returns>Silindi mi.</returns>
    bool Remove(Guid candidateId);

    /// <summary>Konseptin tüm adaylarını siler.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <returns>Silinen adaylar.</returns>
    IReadOnlyList<RevisionCandidate> RemoveForConcept(Guid conceptId);
}
