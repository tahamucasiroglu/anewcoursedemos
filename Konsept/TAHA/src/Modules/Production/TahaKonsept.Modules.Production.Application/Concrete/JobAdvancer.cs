using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan iş ilerletici (scoped; orkestratör her turda yeni kapsamda çağırır).
/// </summary>
/// <param name="dependencies">Bağımlılıklar.</param>
public sealed class JobAdvancer(JobAdvancerDependencies dependencies) : JobAdvancerBase(dependencies);
