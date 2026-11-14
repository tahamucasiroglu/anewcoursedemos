using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// Varlık → sözleşme (DTO) dönüşümleri (elle; AutoMapper kullanılmaz).
/// </summary>
public static class DiagnosticsMappings
{
    /// <summary><see cref="ActivityLog"/> → <see cref="ActivityLogDto"/>.</summary>
    /// <param name="log">Kayıt.</param>
    /// <returns>DTO.</returns>
    public static ActivityLogDto ToDto(this ActivityLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new ActivityLogDto
        {
            Id = log.Id,
            Level = log.Level,
            Source = log.Source,
            Message = log.Message,
            Details = log.Details,
            DurationMs = log.DurationMs,
            UserId = log.UserId,
            CreatedAt = log.CreatedAtUtc,
        };
    }

    /// <summary><see cref="ErrorReport"/> → <see cref="ErrorReportDto"/>.</summary>
    /// <param name="report">Bildirim.</param>
    /// <returns>DTO.</returns>
    public static ErrorReportDto ToDto(this ErrorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new ErrorReportDto
        {
            Id = report.Id,
            UserId = report.UserId,
            Source = report.Source,
            Message = report.Message,
            Details = report.Details,
            PageUrl = report.PageUrl,
            UserAgent = report.UserAgent,
            AppVersion = report.AppVersion,
            Machine = report.Machine,
            Status = report.Status,
            CreatedAt = report.CreatedAtUtc,
            StatusChangedAt = report.StatusChangedAtUtc,
        };
    }
}
