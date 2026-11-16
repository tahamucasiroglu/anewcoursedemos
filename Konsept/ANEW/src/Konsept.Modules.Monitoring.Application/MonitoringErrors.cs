using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application;

/// <summary>Monitoring'in beklenen hataları (kararlı kod + Türkçe mesaj).</summary>
public static class MonitoringErrors
{
    /// <summary>Tek istekte kabul edilen en fazla log kaydı.</summary>
    public const int MaxLogsPerRequest = 200;

    /// <summary>Gets tek istekte çok fazla log kaydı.</summary>
    public static Error TooManyLogs { get; } =
        new("logs.too_many", $"Tek istekte en fazla {MaxLogsPerRequest} log kaydı gönderilebilir.", ErrorType.Validation);

    /// <summary>Gets Günlük isteğinde oturum yok (default-deny'ın arkasında ikinci güvence, spec 0021 Req 7).</summary>
    public static Error SignInRequired { get; } = new("auth.required", "Giriş yapmanız gerekiyor.", ErrorType.Unauthorized);
}
