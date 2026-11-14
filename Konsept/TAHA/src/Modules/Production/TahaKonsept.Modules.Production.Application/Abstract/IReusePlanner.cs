using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Yeni sürüm için yeniden kullanım planı: aşamaları hat sırasıyla benzetir, her aşamanın yeni girdi hash'ini kaynak işinkiyle karşılaştırır;
/// hash'i değişen aşama ve bağımlıları yeniden çalışır (değişen nondeterministik aşamanın çıktı hash'i yapay kabul edilir).
/// </summary>
public interface IReusePlanner
{
    /// <summary>Planı çıkarır (bağlamdaki çıktı görünümü yok sayılır).</summary>
    /// <param name="context">Yeni işin bağlamı.</param>
    /// <param name="baseRuns">Kaynak işin çalıştırmaları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Plan.</returns>
    Task<ReusePlan> PlanAsync(PipelineContext context, IReadOnlyList<StageRun> baseRuns, CancellationToken cancellationToken);
}
