using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Kullanıcının "Üretiliyor" işi var mı (spec 0023 Req 3); davranış <see cref="ProductionActivityReaderBase"/>'dedir.</summary>
/// <param name="jobs">İş deposu.</param>
public sealed class ProductionActivityReader(IProductionJobRepository jobs) : ProductionActivityReaderBase(jobs);
