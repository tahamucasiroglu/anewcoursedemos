using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Production'ın konsept okuma PORTU (Concepts modülüne derleme bağımlılığı olmasın diye). Entegrasyonda Concepts'in
/// <c>IConceptReader.GetAsync(userId, conceptId, ct)</c> sonucunu <see cref="ConceptSnapshot"/>'a çeviren bir adaptör kaydedilir.
/// Kayıt yoksa varsayılan <c>UnavailableConceptSource</c> her zaman <c>null</c> döner (sıra öğesi "konsept bulunamadı" ile düşer).
/// </summary>
public interface IConceptSource
{
    /// <summary>Kullanıcının konseptini okur (yoksa <c>null</c>).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ConceptSnapshot?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
