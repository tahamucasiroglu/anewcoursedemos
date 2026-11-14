using Konsept.Modules.Identity.Application.Base;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Oturum servisi (refresh, doğrulama, çıkış, geçerli kullanıcı, tüm oturumları iptal).</summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="logger">Teknik log.</param>
public sealed class SessionService(AuthServiceDependencies dependencies, ICurrentUser currentUser, ILogger<SessionService> logger)
    : SessionServiceBase(dependencies, currentUser, logger);
