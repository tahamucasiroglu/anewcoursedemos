using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Çıkışta üretimi durduran dinleyici (Identity.Contracts, spec 0006 Req 16).</summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="time">Saat.</param>
public sealed class ProductionSessionListener(IProductionJobRepository jobs, TimeProvider time) : ProductionSessionListenerBase(jobs, time);
