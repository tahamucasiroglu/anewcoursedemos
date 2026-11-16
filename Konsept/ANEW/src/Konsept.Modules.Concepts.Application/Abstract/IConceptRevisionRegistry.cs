using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konsept revizyonları (Api süreci belleğinde; kullanıcı × konsept başına tek revizyon — spec 0015 Req 8, 11). Kalıcı değildir.
/// </summary>
public interface IConceptRevisionRegistry
{
    /// <summary>Yeni revizyonu kaydeder (öncekinin yerine); aynı konseptte süren yeniden üretim varsa kaydetmez (G-3).</summary>
    /// <param name="revision">Revizyon.</param>
    /// <returns>Kaydedildiyse <c>true</c>.</returns>
    bool TryStart(ConceptRevision revision);

    /// <summary>Konseptin revizyonu.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <returns>Revizyon ya da <c>null</c>.</returns>
    ConceptRevision? Get(Guid userId, Guid conceptId);

    /// <summary>Revizyonu kaldırır (yalnızca verilen revizyonsa ve sürmüyorsa).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="revisionId">Revizyon.</param>
    /// <returns>Kaldırıldıysa <c>true</c>.</returns>
    bool Remove(Guid userId, Guid conceptId, Guid revisionId);
}
