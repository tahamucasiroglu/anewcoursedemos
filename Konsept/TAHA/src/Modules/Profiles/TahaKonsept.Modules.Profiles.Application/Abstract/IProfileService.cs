using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Oturumdaki kullanıcının profillerini yöneten uygulama servisi (API controller'ı kullanır).
/// Kurallar: kullanıcı başına tek aktif profil; yeni profil aktif olur; aktif profil silinirse en yeni kalan profil aktifleşir.
/// </summary>
public interface IProfileService : IApplicationService
{
    /// <summary>Kullanıcının profilleri (oluşturulma sırasıyla).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profiller.</returns>
    Task<Result<IReadOnlyList<ProfileDto>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Hazır şablonlar.</summary>
    /// <returns>Şablonlar.</returns>
    Result<IReadOnlyList<ProfileTemplateDto>> GetTemplates();

    /// <summary>Kullanıcının tek profili.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da NotFound.</returns>
    Task<Result<ProfileDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Yeni profil oluşturur ve aktif yapar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan profil.</returns>
    Task<Result<ProfileDto>> CreateAsync(SaveProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Profili günceller.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel profil ya da NotFound.</returns>
    Task<Result<ProfileDto>> UpdateAsync(Guid id, SaveProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Profili aktif yapar; kullanıcının diğer profilleri pasifleşir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profil ya da NotFound.</returns>
    Task<Result<ProfileDto>> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Profili siler; aktifse en yeni kalan profil aktifleşir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
