using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// AgentGateway.Contracts <see cref="IProductionActivityReader"/> (spec 0023 Req 3): kullanıcının "Üretiliyor" durumunda bir işi var mı.
/// "Ajan bekleniyor" sayılmaz (ajan zaten iş almıyor; güncelleme o işi bekletmez).
/// </summary>
/// <param name="jobs">İş deposu.</param>
public abstract class ProductionActivityReaderBase(IProductionJobRepository jobs) : IProductionActivityReader
{
    /// <inheritdoc />
    public async Task<bool> IsProducingAsync(Guid userId, CancellationToken cancellationToken) =>
        (await jobs.ListActiveAsync(userId, cancellationToken).ConfigureAwait(false)).Any(j => j.Status == ProductionJobStatus.Producing);
}
