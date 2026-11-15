using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Açılış rol senkronu.</summary>
/// <param name="users">Kullanıcı deposu.</param>
/// <param name="options">Identity ayarları.</param>
/// <param name="logger">Teknik log.</param>
public sealed class AdminRoleSync(IUserRepository users, IdentityOptions options, ILogger<AdminRoleSync> logger)
    : AdminRoleSyncBase(users, options, logger);
