using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Bir üretim işini yürütür (konu önerisi, seçilenlerden ya da fikirden konsept; spec 0014 Req 2–21).</summary>
public interface IConceptJobExecutor
{
    /// <summary>İşi sonuna kadar yürütür; ilerlemeyi işe yazar, bitince Günlük olayı yazar.</summary>
    /// <param name="job">İş.</param>
    /// <param name="input">Girdi.</param>
    /// <param name="cancellationToken">Uygulama kapanışı.</param>
    /// <returns>Görev.</returns>
    Task ExecuteAsync(ConceptJob job, ConceptJobInput input, CancellationToken cancellationToken);
}
