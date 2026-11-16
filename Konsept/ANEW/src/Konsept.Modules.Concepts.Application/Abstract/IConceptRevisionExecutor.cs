using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Talimatla yeniden üretimi yürütür (spec 0015 Req 7, 10, 12).</summary>
public interface IConceptRevisionExecutor
{
    /// <summary>Yeniden üretir; sonucu revizyona yazar (hazır ya da başarısız).</summary>
    /// <param name="revision">Revizyon.</param>
    /// <param name="input">Girdi.</param>
    /// <param name="cancellationToken">Uygulama kapanışı.</param>
    /// <returns>Görev.</returns>
    Task ExecuteAsync(ConceptRevision revision, RevisionInput input, CancellationToken cancellationToken);
}
