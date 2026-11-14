using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Tek bir konsept üretim işini çalıştırır (scoped; arka plan işçisi her iş için yeni scope açar): bağlam → (fikirse açılar) →
/// her öğe için üret → kelime denetimi → kaydet → hafıza → ilerleme; sonunda işlem günlüğüne süreli kayıt.
/// </summary>
public interface IConceptJobProcessor
{
    /// <summary>İşi çalıştırır.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci (kullanıcı iptali dahil).</param>
    /// <returns>Görev.</returns>
    Task ProcessAsync(ConceptJob job, CancellationToken cancellationToken);
}
