using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Base;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Profiles.Application.Concrete;

/// <summary>Profil servisi.</summary>
/// <param name="profiles">Profil deposu.</param>
/// <param name="templates">Şablonlar.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public sealed class ProfileService(
    IProfileRepository profiles,
    IProfileTemplateSource templates,
    ICurrentUser currentUser,
    TimeProvider time,
    ILogger<ProfileService> logger) : ProfileServiceBase(profiles, templates, currentUser, time, logger);
