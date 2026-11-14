using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Mapping;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Base;

/// <summary>
/// <see cref="IProfileReader"/> ortak davranışı: depodan takipsiz okur ve DTO'ya çevirir.
/// </summary>
public abstract class ProfileReaderBase : IProfileReader
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Profil deposu.</param>
    protected ProfileReaderBase(IProfileRepository repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>Profil deposu.</summary>
    protected IProfileRepository Repository { get; }

    /// <summary>Kullanıcının aktif profili.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public virtual async Task<ProfileDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await Repository.GetActiveAsync(userId, cancellationToken))?.ToDto();

    /// <summary>Kimliğe göre profil.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public virtual async Task<ProfileDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await Repository.FindAsync(id, cancellationToken))?.ToDto();
}
