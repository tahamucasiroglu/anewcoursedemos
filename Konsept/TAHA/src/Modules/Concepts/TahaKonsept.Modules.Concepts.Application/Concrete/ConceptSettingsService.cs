using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan ayar servisi; davranış <see cref="ConceptSettingsServiceBase"/>'tedir.
/// </summary>
/// <param name="repository">Ayar deposu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="contexts">Bağlam kurucu.</param>
public sealed class ConceptSettingsService(IConceptSettingsRepository repository, ICurrentUser currentUser, IProfileReader profiles, IConceptContextProvider contexts)
    : ConceptSettingsServiceBase(repository, currentUser, profiles, contexts);
