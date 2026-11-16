using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Base;

namespace Konsept.Modules.Profiles.Application.Concrete;

/// <summary>Konsept üretimi için profil bağlamı (Profiles.Contracts, spec 0014); davranış <see cref="ProfileGenerationReaderBase"/>'tedir.</summary>
/// <param name="profiles">Profil deposu.</param>
/// <param name="settings">Konsept ayarı deposu.</param>
public sealed class ProfileGenerationReader(IProfileRepository profiles, IConceptSettingsRepository settings)
    : ProfileGenerationReaderBase(profiles, settings);
