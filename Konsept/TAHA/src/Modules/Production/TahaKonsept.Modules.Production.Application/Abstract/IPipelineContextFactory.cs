using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Bir işten <see cref="PipelineContext"/> kurar: ayar/konsept anlık görüntülerini çözer, okunuş sözlüğünü yükler, çıktı görünümünü işin çalıştırmalarından oluşturur.
/// </summary>
public interface IPipelineContextFactory
{
    /// <summary>Bağlamı kurar.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<PipelineContext> CreateAsync(Job job, CancellationToken cancellationToken);
}
