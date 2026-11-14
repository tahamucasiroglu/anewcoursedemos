using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Base;

namespace TahaKonsept.Modules.Profiles.Application.Concrete;

/// <summary>
/// Varsayılan profil servisi; tüm davranış <see cref="ProfileServiceBase"/>'tedir.
/// </summary>
/// <param name="repository">Profil deposu.</param>
/// <param name="templates">Şablon kataloğu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
public sealed class ProfileService(IProfileRepository repository, IProfileTemplateCatalog templates, ICurrentUser currentUser)
    : ProfileServiceBase(repository, templates, currentUser);
