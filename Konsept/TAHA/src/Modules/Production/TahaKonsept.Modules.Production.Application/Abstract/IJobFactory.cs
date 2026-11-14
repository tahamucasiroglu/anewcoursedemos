using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Sıra öğesinden iş oluşturur: konsepti (ya da geçersiz kılmayı) okur, ayarları anlık görüntüler, çözünürlüğü biçimden seçer, aşama çalıştırmalarını
/// ekler, yeni sürümde kaynak işi bağlar ve çalışma alanının paylaşılıp paylaşılmayacağına karar verir. İş depoya eklenir (kaydetmez).
/// </summary>
public interface IJobFactory
{
    /// <summary>İşi oluşturur.</summary>
    /// <param name="item">Sıra öğesi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<Job>> CreateAsync(QueueItem item, CancellationToken cancellationToken);
}
