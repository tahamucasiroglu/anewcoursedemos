using Konsept.Modules.Monitoring.Domain.Rules;
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

    /// <summary>Gets saatlik hata bildirimi sınırı aşıldı (spec 0022 Req 15).</summary>
    public static Error ReportRateLimited { get; } =
        new("error_report.rate_limited", "Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin", ErrorType.TooManyRequests);

    /// <summary>Gets hata bildirimi bulunamadı.</summary>
    public static Error ReportNotFound { get; } = new("error_report.not_found", "Hata bildirimi bulunamadı.", ErrorType.NotFound);

    /// <summary>Açıklamanın doğrulama hatası (spec 0022 Req 1, AC-3): boş, 10 karakterden kısa ya da 4000'den uzun.</summary>
    /// <param name="description">Açıklama.</param>
    /// <returns>Hata (alan hatasıyla) ya da geçerliyse <c>null</c>.</returns>
    public static Error? DescriptionError(string? description)
    {
        var length = (description ?? string.Empty).Trim().Length;
        var message = length switch
        {
            0 => "Lütfen karşılaştığınız sorunu yazın.",
            < ErrorReportRules.DescriptionMinLength => $"Açıklama en az {ErrorReportRules.DescriptionMinLength} karakter olmalı.",
            > ErrorReportRules.DescriptionMaxLength => $"Açıklama en fazla {ErrorReportRules.DescriptionMaxLength} karakter olabilir.",
            _ => null,
        };
        return message is null
            ? null
            : new Error("error_report.description", message, ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]>(StringComparer.Ordinal) { ["description"] = [message] },
            };
    }
}
