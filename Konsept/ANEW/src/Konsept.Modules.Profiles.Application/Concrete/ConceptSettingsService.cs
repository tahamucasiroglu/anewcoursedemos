using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Base;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Profiles.Application.Concrete;

/// <summary>Konsept ayarları servisi.</summary>
/// <param name="profiles">Profil deposu.</param>
/// <param name="settings">Ayar deposu.</param>
/// <param name="models">Metin modeli dizini.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptSettingsService(
    IProfileRepository profiles,
    IConceptSettingsRepository settings,
    ITextModelDirectory models,
    ICurrentUser currentUser,
    TimeProvider time) : ConceptSettingsServiceBase(profiles, settings, models, currentUser, time);
