using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Errors;
using TahaKonsept.Modules.Profiles.Application.Mapping;
using TahaKonsept.Modules.Profiles.Domain.Entities;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Base;

/// <summary>
/// <see cref="IProfileService"/> ortak davranışı: tüm işlemler oturumdaki kullanıcının (<see cref="ICurrentUser"/>) kayıtlarıyla sınırlıdır.
/// Tek aktif profil kuralı <see cref="MakeExclusiveActiveAsync"/> içinde uygulanır (diğer aktifler pasifleşir, hepsi tek kayıtta).
/// </summary>
public abstract class ProfileServiceBase : IProfileService
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Profil deposu.</param>
    /// <param name="templates">Şablon kataloğu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    protected ProfileServiceBase(IProfileRepository repository, IProfileTemplateCatalog templates, ICurrentUser currentUser)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Templates = templates ?? throw new ArgumentNullException(nameof(templates));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Profil deposu.</summary>
    protected IProfileRepository Repository { get; }

    /// <summary>Şablon kataloğu.</summary>
    protected IProfileTemplateCatalog Templates { get; }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Kullanıcının profilleri.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profiller.</returns>
    public virtual async Task<Result<IReadOnlyList<ProfileDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await Repository.ListForUserAsync(CurrentUser.GetRequiredUserId(), cancellationToken);
        return Result.Success<IReadOnlyList<ProfileDto>>(profiles.Select(p => p.ToDto()).ToList());
    }

    /// <summary>Hazır şablonlar.</summary>
    /// <returns>Şablonlar.</returns>
    public virtual Result<IReadOnlyList<ProfileTemplateDto>> GetTemplates() => Result.Success(Templates.GetAll());

    /// <summary>Kullanıcının tek profili.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da NotFound.</returns>
    public virtual async Task<Result<ProfileDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await Repository.GetForUserTrackedAsync(id, CurrentUser.GetRequiredUserId(), cancellationToken);
        return profile is null ? ProfileErrors.NotFound : profile.ToDto();
    }

    /// <summary>Yeni profil oluşturur ve aktif yapar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan profil.</returns>
    public virtual async Task<Result<ProfileDto>> CreateAsync(SaveProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = CurrentUser.GetRequiredUserId();
        var profile = Profile.Create(userId, request.ToDetails());
        await Repository.AddAsync(profile, cancellationToken);
        await MakeExclusiveActiveAsync(userId, profile, cancellationToken);
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }

    /// <summary>Profili günceller.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel profil ya da NotFound.</returns>
    public virtual async Task<Result<ProfileDto>> UpdateAsync(Guid id, SaveProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profile = await Repository.GetForUserTrackedAsync(id, CurrentUser.GetRequiredUserId(), cancellationToken);
        if (profile is null)
        {
            return ProfileErrors.NotFound;
        }

        profile.Update(request.ToDetails());
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }

    /// <summary>Profili aktif yapar; diğerleri pasifleşir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profil ya da NotFound.</returns>
    public virtual async Task<Result<ProfileDto>> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var profile = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (profile is null)
        {
            return ProfileErrors.NotFound;
        }

        await MakeExclusiveActiveAsync(userId, profile, cancellationToken);
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }

    /// <summary>Profili siler; aktifse en yeni kalan profil aktifleşir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var profile = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure(ProfileErrors.NotFound);
        }

        if (profile.IsActive)
        {
            var next = await Repository.GetNewestOtherTrackedAsync(userId, profile.Id, cancellationToken);
            next?.Activate();
        }

        Repository.Remove(profile);
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Hedef profili aktif yapar, kullanıcının diğer aktif profillerini pasifleştirir (kaydetmez).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="target">Aktif olacak profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual async Task MakeExclusiveActiveAsync(Guid userId, Profile target, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        var actives = await Repository.ListActiveTrackedAsync(userId, cancellationToken);
        foreach (var other in actives.Where(p => p.Id != target.Id))
        {
            other.Deactivate();
        }

        target.Activate();
    }
}
