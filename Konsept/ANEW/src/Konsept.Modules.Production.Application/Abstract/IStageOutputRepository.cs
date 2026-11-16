using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>Konsept başına aşama çıktıları (spec 0017 Req 10); her işlem kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface IStageOutputRepository
{
    /// <summary>Konseptin çıktıları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Çıktılar ya da <c>null</c>.</returns>
    Task<StageOutputs?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Çıktıları yazar (ekler ya da değiştirir).</summary>
    /// <param name="outputs">Çıktılar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task UpsertAsync(StageOutputs outputs, CancellationToken cancellationToken);
}
