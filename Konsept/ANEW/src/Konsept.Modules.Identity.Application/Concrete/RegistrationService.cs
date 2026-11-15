using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Kayıt servisi.</summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
/// <param name="options">Identity ayarları (ilk admin e-postası).</param>
public sealed class RegistrationService(AuthServiceDependencies dependencies, IdentityOptions? options = null)
    : RegistrationServiceBase(dependencies, options);
