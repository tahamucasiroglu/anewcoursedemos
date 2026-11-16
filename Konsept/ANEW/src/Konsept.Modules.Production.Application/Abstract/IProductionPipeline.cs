using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretim hattı (spec 0017): kullanıcının çalışan işini aşama aşama sürdürür; iş biterse ya da hata verirse aynı profilin sıradaki işini
/// başlatır (art arda 3 hatada durur).
/// </summary>
public interface IProductionPipeline
{
    /// <summary>Çalışan işi bir kez sürdürür.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="caller">Çağıran kimliği (online çağrılar bununla).</param>
    /// <param name="stop">Durdur belirteci (yalnızca yerel aşamayı keser; süren online çağrı tamamlanır).</param>
    /// <returns>Yeni bir iş başladıysa <c>true</c> (yeniden çağrılmalı).</returns>
    Task<bool> RunAsync(Guid userId, ModelCallerIdentity caller, CancellationToken stop);
}
