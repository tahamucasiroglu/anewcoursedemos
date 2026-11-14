using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Abstract;

/// <summary>
/// Ön gereksinim kurulumunu yönetir (kurulum ekranı bunu kullanır): durum listesi, zorunlular + seçilen isteğe bağlılar sırayla,
/// tek bileşen kurulumu. İlerleme <see cref="PrerequisiteProgress"/> (adım adı, yüzde, günlük satırı) ile bildirilir.
/// </summary>
public interface IPrerequisiteInstaller
{
    /// <summary>Tüm bileşenlerin durumu (kurulum sırasına göre).</summary>
    /// <returns>Liste.</returns>
    IReadOnlyList<PrerequisiteStatus> GetStatus();

    /// <summary>Zorunlu bileşenler kurulu mu.</summary>
    /// <returns>Hepsi kuruluysa <c>true</c>.</returns>
    bool AreRequiredInstalled();

    /// <summary>Eksik zorunlu bileşenleri ve seçilen isteğe bağlı bileşenleri kurar. Zorunlu bir bileşen başarısız olursa durur.</summary>
    /// <param name="optionalIds">Kurulacak isteğe bağlı bileşen kimlikleri (boş/null = hiçbiri).</param>
    /// <param name="progress">İlerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç (başarısız bileşenler mesajda).</returns>
    Task<Result> InstallAsync(IReadOnlyCollection<string>? optionalIds, IProgress<PrerequisiteProgress>? progress, CancellationToken ct);

    /// <summary>Tek bileşeni kurar.</summary>
    /// <param name="id">Bileşen kimliği.</param>
    /// <param name="progress">İlerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> InstallOneAsync(string id, IProgress<PrerequisiteProgress>? progress, CancellationToken ct);
}
