using System.Globalization;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Hata bildirimlerinin ortak mantığı (spec 0022 Req 1, 4, 10–15): kullanıcı yalnızca oturumdan (BR-1); açıklama 10–4000; web + ajan
/// toplamı saatte 10 (API kayıtları sayılmaz); web bildirimine son 20 Günlük kaydı ve versiyon sunucuda eklenir; kabul edilen web/ajan
/// bildirimi Günlük'e Bilgi "Hata bildirimi gönderildi" yazar. Kalıcılık ve bağlam alt sınıftadır.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class ErrorReportServiceBase(TimeProvider timeProvider) : IErrorReportService
{
    /// <summary>Günlük'e düşen olayın türü.</summary>
    public const string SentEventType = "error_report.sent";

    /// <summary>Günlük'e düşen olayın metni (Req 14).</summary>
    public const string SentEventMessage = "Hata bildirimi gönderildi";

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Gets isteği yapan kullanıcı; giriş yapılmamışsa <c>null</c>.</summary>
    protected abstract Guid? CurrentUserId { get; }

    /// <summary>Gets ürün versiyonu.</summary>
    protected abstract string ProductVersion { get; }

    /// <summary>Kaynak adının sözleşme karşılığı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <returns><c>web</c> / <c>agent</c> / <c>api</c>.</returns>
    public static string SourceName(ErrorReportSource source) => source switch
    {
        ErrorReportSource.Web => "web",
        ErrorReportSource.Agent => "agent",
        _ => "api",
    };

    /// <summary>Bildirimi sözleşme karşılığına çevirir.</summary>
    /// <param name="r">Bildirim.</param>
    /// <returns>DTO.</returns>
    public static ErrorReportDto ToDto(ErrorReport r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new ErrorReportDto(
            r.Id,
            r.Number,
            SourceName(r.Source),
            "new",
            r.CreatedAt,
            r.UserId,
            r.ProductVersion,
            r.Description,
            r.Page,
            r.CorrelationId,
            r.AgentVersion,
            r.OperatingSystem,
            r.Component,
            r.Step,
            r.Attachments);
    }

    /// <inheritdoc />
    public async Task<Result<ErrorReportPreviewDto>> GetPreviewAsync(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Result<ErrorReportPreviewDto>.Failure(MonitoringErrors.SignInRequired);
        }

        var journal = await RecentJournalAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result<ErrorReportPreviewDto>.Success(new ErrorReportPreviewDto(
            ProductVersion,
            TimeProvider.GetUtcNow(),
            journal.Select(SystemEventServiceBase.ToDto).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<ErrorReportReceiptDto>> SubmitWebAsync(WebErrorReportRequest? request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Result<ErrorReportReceiptDto>.Failure(MonitoringErrors.SignInRequired);
        }

        if (MonitoringErrors.DescriptionError(request?.Description) is { } invalid)
        {
            return Result<ErrorReportReceiptDto>.Failure(invalid);
        }

        if (await IsRateLimitedAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ErrorReportReceiptDto>.Failure(MonitoringErrors.ReportRateLimited);
        }

        var journal = await RecentJournalAsync(userId, cancellationToken).ConfigureAwait(false);
        var report = ErrorReport.CreateWeb(
            userId,
            TimeProvider.GetUtcNow(),
            ProductVersion,
            request!.Description!,
            request.Page,
            request.CorrelationId,
            journal.Select(JournalLine));
        await AcceptAsync(report, userId, cancellationToken).ConfigureAwait(false);
        return Result<ErrorReportReceiptDto>.Success(new ErrorReportReceiptDto(report.Number));
    }

    /// <inheritdoc />
    public async Task<ErrorReportSubmission> SubmitFromAgentAsync(Guid userId, AgentErrorReportEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(entry.Summary))
        {
            return new ErrorReportSubmission(ErrorReportOutcome.Invalid);
        }

        if (await IsRateLimitedAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            return new ErrorReportSubmission(ErrorReportOutcome.RateLimited);
        }

        var report = ErrorReport.CreateAgent(
            userId,
            TimeProvider.GetUtcNow(),
            ProductVersion,
            entry.Summary,
            entry.AgentVersion,
            entry.OperatingSystem,
            entry.Component,
            entry.Step,
            entry.LogLines ?? []);
        await AcceptAsync(report, userId, cancellationToken).ConfigureAwait(false);
        return new ErrorReportSubmission(ErrorReportOutcome.Accepted, report.Number);
    }

    /// <inheritdoc />
    public async Task<Result<ErrorReportDto>> GetAsync(long number, CancellationToken cancellationToken)
    {
        var report = await LoadAsync(number, cancellationToken).ConfigureAwait(false);
        return report is null
            ? Result<ErrorReportDto>.Failure(MonitoringErrors.ReportNotFound)
            : Result<ErrorReportDto>.Success(ToDto(report));
    }

    /// <inheritdoc />
    public Task RecordUnexpectedAsync(string summary, string? correlationId, CancellationToken cancellationToken) =>
        PersistAsync(ErrorReport.CreateApi(CurrentUserId, TimeProvider.GetUtcNow(), ProductVersion, summary, correlationId), cancellationToken);

    /// <summary>Bildirimi kaydeder.</summary>
    /// <param name="report">Bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task PersistAsync(ErrorReport report, CancellationToken cancellationToken);

    /// <summary>Numarayla bildirimi okur.</summary>
    /// <param name="number">Numara.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim ya da <c>null</c>.</returns>
    protected abstract Task<ErrorReport?> LoadAsync(long number, CancellationToken cancellationToken);

    /// <summary>Kullanıcının pencere içindeki web + ajan bildirimlerini sayar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="since">Pencere başı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    protected abstract Task<int> CountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Kullanıcının son Günlük kayıtlarını (en yenisi üstte) okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="count">Kayıt sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    protected abstract Task<IReadOnlyList<SystemEvent>> ReadJournalAsync(Guid userId, int count, CancellationToken cancellationToken);

    /// <summary>Kullanıcının Günlük'üne "Hata bildirimi gönderildi" yazar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task WriteSentEventAsync(Guid userId, CancellationToken cancellationToken);

    private static string JournalLine(SystemEvent e) =>
        string.Create(CultureInfo.InvariantCulture, $"{e.OccurredAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ} [{e.Level}] {e.Message}");

    private Task<IReadOnlyList<SystemEvent>> RecentJournalAsync(Guid userId, CancellationToken cancellationToken) =>
        ReadJournalAsync(userId, ErrorReportRules.JournalEntryCount, cancellationToken);

    private async Task<bool> IsRateLimitedAsync(Guid userId, CancellationToken cancellationToken) =>
        await CountSinceAsync(userId, TimeProvider.GetUtcNow() - ErrorReportRules.RateWindow, cancellationToken).ConfigureAwait(false)
            >= ErrorReportRules.HourlyLimit;

    private async Task AcceptAsync(ErrorReport report, Guid userId, CancellationToken cancellationToken)
    {
        await PersistAsync(report, cancellationToken).ConfigureAwait(false);
        await WriteSentEventAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
