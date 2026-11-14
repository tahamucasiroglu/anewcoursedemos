using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Kayıt servisi.</summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
public sealed class RegistrationService(AuthServiceDependencies dependencies) : RegistrationServiceBase(dependencies);
