using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Son aşama (Mux) bitince videoyu kaydeder: sahneler (yazılış/okunuş/süre), maliyet dökümü, iş/öğe tamamlandı, online artefakt temizliği,
/// işlem günlüğüne "Video hazırlandı — süre … sn, başlangıç …, bitiş …, $…".
/// </summary>
public interface IJobFinalizer
{
    /// <summary>Videoyu oluşturur ve kaydeder.</summary>
    /// <param name="job">İş.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Video> FinalizeAsync(Job job, PipelineContext context, CancellationToken cancellationToken);
}
