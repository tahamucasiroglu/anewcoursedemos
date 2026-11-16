using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretilen Videolar (spec 0007): profilin videoları, seçilen videonun sürümleri ve dosya eylemleri. İsteği yapan kullanıcıyla çalışır;
/// başka kullanıcının profili, videosu ya da sürümü "bulunamadı"dır ve ajana komut gitmez (BR-1, Req 10).
/// </summary>
public interface IProducedVideoService
{
    /// <summary>Profilin en az bir sürümü tamamlanmış videoları, üretim tarihine göre en yeni üstte, 50'şerli.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="skip">Atlanacak öğe sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste parçası.</returns>
    Task<Result<ProducedLibraryPageDto>> ListAsync(Guid profileId, int skip, CancellationToken cancellationToken);

    /// <summary>Videonun sürümleri ve toplam maliyeti.</summary>
    /// <param name="videoId">Video (konsept).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    Task<Result<ProducedVideoDetailDto>> GetAsync(Guid videoId, CancellationToken cancellationToken);

    /// <summary>Sürümün dosyası için ajana Klasörde aç / Sistem oynatıcısında aç komutu; ajan 10 sn'de cevap vermezse zaman aşımı.</summary>
    /// <param name="versionId">Sürüm.</param>
    /// <param name="request">Eylem.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderilen eylem ya da hata.</returns>
    Task<Result<FileActionRequest>> OpenFileAsync(Guid versionId, FileActionRequest request, CancellationToken cancellationToken);
}
