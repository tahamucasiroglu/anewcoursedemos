using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Profiles.Application.Abstract;

/// <summary>İsteği yapan kullanıcının bir profiline ait Konseptler sayfası ayarları (spec 0005). Başka kullanıcının profili 404'tür.</summary>
public interface IConceptSettingsService
{
    /// <summary>Ayarlar ve seçilebilir modeller.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar · 404 · 401.</returns>
    Task<Result<ConceptSettingsDto>> GetAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>Ayarları kaydeder; geçersiz istek hiçbir şeyi değiştirmez.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni ayarlar · 400 · 404 · 401.</returns>
    Task<Result<ConceptSettingsDto>> SaveAsync(Guid profileId, SaveConceptSettingsRequest? request, CancellationToken cancellationToken);
}
