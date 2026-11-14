using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Common;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Modules.Diagnostics.Domain.Errors;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// <see cref="IErrorReportService"/> uygulaması. Gönderimde sürüm boşsa sunucu sürümünü yazar ve günlüğe uyarı düşer;
/// listeleme ve durum değişikliği yalnızca yönetici içindir (durum akışı <see cref="ErrorReport.ChangeStatus"/>'ta).
/// </summary>
public sealed class ErrorReportService : DiagnosticsServiceBase, IErrorReportService
{
    private static readonly string ServerVersion = AppVersion.Of(typeof(ErrorReportService).Assembly);
    private readonly IErrorReportRepository _reports;
    private readonly IActivityLogger _activity;
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="reports">Bildirim deposu.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    public ErrorReportService(IErrorReportRepository reports, IActivityLogger activity, IClock clock, ICurrentUser currentUser)
        : base(currentUser)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Bildirim oluşturur.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan bildirim.</returns>
    public async Task<Result<ErrorReportDto>> CreateAsync(ErrorReportCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var report = ErrorReport.Create(
            CurrentUserIdOrEmpty,
            request.Source,
            request.Message,
            request.Details,
            request.PageUrl,
            request.UserAgent,
            string.IsNullOrWhiteSpace(request.AppVersion) ? ServerVersion : request.AppVersion,
            request.Machine);
        await _reports.AddAsync(report, cancellationToken);
        await _reports.UnitOfWork.SaveChangesAsync(cancellationToken);

        var summary = report.Message.Length > 120 ? report.Message[..119] + "…" : report.Message;
        await _activity.WarningAsync(
            ActivitySources.Diagnostics,
            $"Hata bildirimi alındı ({SourceLabel(report.Source)}): {summary}",
            new { reportId = report.Id, source = report.Source.ToString() },
            report.UserId,
            cancellationToken);
        return report.ToDto();
    }

    /// <summary>Bildirimleri listeler (yönetici).</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    public async Task<Result<PagedResult<ErrorReportDto>>> ListAsync(ErrorReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        var status = query.Status;
        var page = await _reports.GetPagedAsync(
            new PagedRequest { Page = query.Page, PageSize = ClampTake(query.PageSize, 50) },
            r => status == null || r.Status == status,
            q => q.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id),
            cancellationToken);
        return page.Map(r => r.ToDto());
    }

    /// <summary>Durum değiştirir (yönetici).</summary>
    /// <param name="id">Bildirim.</param>
    /// <param name="request">Yeni durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel bildirim.</returns>
    public async Task<Result<ErrorReportDto>> ChangeStatusAsync(Guid id, ErrorReportStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return DiagnosticsErrors.ErrorReportNotFound;
        }

        var previous = report.Status;
        var changed = report.ChangeStatus(request.Status, _clock.UtcNow);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        if (previous != report.Status)
        {
            await _reports.UnitOfWork.SaveChangesAsync(cancellationToken);
            await _activity.InformationAsync(
                ActivitySources.Diagnostics,
                $"Hata bildirimi durumu değişti: {StatusLabel(previous)} → {StatusLabel(report.Status)}",
                new { reportId = report.Id },
                cancellationToken: cancellationToken);
        }

        return report.ToDto();
    }

    private static string StatusLabel(ErrorReportStatus status) => status switch
    {
        ErrorReportStatus.New => "Yeni",
        ErrorReportStatus.Seen => "Görüldü",
        ErrorReportStatus.Resolved => "Çözüldü",
        _ => status.ToString(),
    };

    private static string SourceLabel(ErrorReportSource source) => source switch
    {
        ErrorReportSource.Web => "Web",
        ErrorReportSource.Agent => "Ajan",
        ErrorReportSource.Installer => "Kurulum",
        ErrorReportSource.Api => "API",
        _ => source.ToString(),
    };
}
