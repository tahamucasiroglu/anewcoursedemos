using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan iş sorgu servisi (scoped).
/// </summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
public sealed class JobQueryService(IJobRepository jobs, ICurrentUser currentUser) : JobQueryServiceBase(jobs, currentUser);
