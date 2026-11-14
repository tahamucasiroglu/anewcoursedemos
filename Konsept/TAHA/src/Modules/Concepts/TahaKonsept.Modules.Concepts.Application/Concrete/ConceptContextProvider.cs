using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan bağlam kurucu; davranış <see cref="ConceptContextProviderBase"/>'tedir.
/// </summary>
/// <param name="settings">Ayar deposu.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="promptBuilder">Profil prompt kurucu.</param>
public sealed class ConceptContextProvider(IConceptSettingsRepository settings, IProfileReader profiles, IProfilePromptBuilder promptBuilder)
    : ConceptContextProviderBase(settings, profiles, promptBuilder);
