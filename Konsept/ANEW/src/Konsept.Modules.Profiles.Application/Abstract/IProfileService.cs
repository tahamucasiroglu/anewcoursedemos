using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Profiles.Application.Abstract;

/// <summary>İsteği yapan kullanıcının profilleri (spec 0003). Başka kullanıcının profili her işlemde "bulunamadı"dır (AC-15).</summary>
public interface IProfileService
{
    /// <summary>Profiller, seçili profil, sınır ve şablonlar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    Task<Result<ProfileListDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Profil oluşturur ve seçili profil yapar (Req 3).</summary>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil · 400 alan hatası · 409 sınır.</returns>
    Task<Result<ProfileDto>> CreateAsync(SaveProfileRequest? request, CancellationToken cancellationToken);

    /// <summary>Profili düzenler (Req 4).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil · 400 · 404.</returns>
    Task<Result<ProfileDto>> UpdateAsync(Guid profileId, SaveProfileRequest? request, CancellationToken cancellationToken);

    /// <summary>Profili kalıcı siler (Req 14).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni liste (seçili profil Req 9'a göre) · 404.</returns>
    Task<Result<ProfileListDto>> DeleteAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>Seçili profili değiştirir (Req 8–9).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seçilen profil · 404.</returns>
    Task<Result<ProfileDto>> SelectAsync(SelectProfileRequest? request, CancellationToken cancellationToken);
}
