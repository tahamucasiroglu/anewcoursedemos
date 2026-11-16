using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Modules.Profiles.Application.Abstract;

/// <summary>Konsept ayarlarının kalıcılığı; her işlem kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface IConceptSettingsRepository
{
    /// <summary>Profilin ayarları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar; satır yoksa ya da başkasınınsa <c>null</c>.</returns>
    Task<ConceptSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Ayarları yazar (profil başına tek satır; başka kullanıcının satırının üzerine yazmaz).</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SaveAsync(ConceptSettings settings, CancellationToken cancellationToken);
}
