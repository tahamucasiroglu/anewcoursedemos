using Konsept.Modules.Identity.Application.Base;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Admin Kullanıcılar servisi (spec 0024): olay yazılamazsa uyarı loglar.</summary>
/// <param name="dependencies">Bağımlılıklar.</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class AdminUserService(AdminUserDependencies dependencies, ILogger<AdminUserService> logger) : AdminUserServiceBase(dependencies)
{
    /// <inheritdoc />
    protected override void OnEventWriteFailed(string type, Exception exception) => LogEventWriteFailed(logger, type, exception);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Warning, Message = "Ban olayı yazılamadı: {EventType}")]
    private static partial void LogEventWriteFailed(ILogger logger, string eventType, Exception exception);
}
