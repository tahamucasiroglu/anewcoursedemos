using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Oturumdaki kullanıcının konsept üretim ayarları (kayıt yoksa varsayılanlar döner; ilk kaydetmede oluşur).
/// </summary>
public interface IConceptSettingsService : IApplicationService
{
    /// <summary>Ayarları getirir (<c>EffectiveFactGrounding</c> aktif profile göre doldurulur).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar.</returns>
    Task<Result<ConceptSettingsDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedilen ayarlar.</returns>
    Task<Result<ConceptSettingsDto>> SaveAsync(ConceptSettingsDto settings, CancellationToken cancellationToken = default);
}
