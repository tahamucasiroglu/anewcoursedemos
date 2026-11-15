using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Açılış rol senkronu: ayardaki e-posta <see cref="AdminEmailPolicy"/> ile normalleştirilir ve depo tek atomik işlemle
/// rolleri eşitler. Rolü değişen hesap sayısı Information olarak yazılır; e-posta loglanmaz.
/// </summary>
public abstract partial class AdminRoleSyncBase : IAdminRoleSync
{
    private readonly IUserRepository _users;
    private readonly IdentityOptions _options;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AdminRoleSyncBase"/> class.</summary>
    /// <param name="users">Kullanıcı deposu.</param>
    /// <param name="options">Identity ayarları.</param>
    /// <param name="logger">Teknik log.</param>
    protected AdminRoleSyncBase(IUserRepository users, IdentityOptions options, ILogger logger)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        var normalized = AdminEmailPolicy.NormalizedAdminEmail(_options.AdminEmail);
        var changed = await _users.SyncAdminRoleAsync(normalized, cancellationToken).ConfigureAwait(false);
        LogSynced(_logger, normalized is not null, changed);
        return changed;
    }

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Admin rol senkronu: ayar {AdminConfigured}, rolü değişen hesap {Changed}")]
    private static partial void LogSynced(ILogger logger, bool adminConfigured, int changed);
}
