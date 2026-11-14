using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Giriş servisi.</summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
public sealed class LoginService(AuthServiceDependencies dependencies) : LoginServiceBase(dependencies);
