using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Üretim bağlamını kurar: kullanıcı ayarları + profil (verilen ya da aktif) + profil prompt metni → <see cref="ConceptGenerationContext"/>.
/// Gerçek bilgi otomatik kararı <see cref="IsFactHeavy"/> ile (tarih, spor, bilim, gizem… kanalları).
/// </summary>
public interface IConceptContextProvider
{
    /// <summary>Bağlamı kurar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil (null ya da bulunamazsa aktif profil).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam ya da NoProfile.</returns>
    Task<Result<ConceptGenerationContext>> ResolveAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default);

    /// <summary>Profil gerçek bilgi gerektiren bir alanda mı.</summary>
    /// <param name="profile">Profil.</param>
    /// <returns>Gerektiriyor mu.</returns>
    bool IsFactHeavy(ProfileDto? profile);
}
