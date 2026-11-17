using Konsept.Modules.Identity.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Admin Hata bildirimleri mantığı (spec 0025 Req 1–6): filtreler (kaynak Tümü/web/ajan/API; durum Açık/Yeni/İnceleniyor/Çözüldü/
/// Yok sayıldı/Tümü; bilinmeyen değer varsayılana döner), 25'erli sayfa, bildiren adı/e-postası (yoksa "—"), kısa mesaj, durum ve not
/// doğrulaması. Satır ve detay yalnızca bildirimin kendi içeriğini taşır (BR-1). Kalıcılık ve kullanıcı dizini alt sınıftadır.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class AdminErrorReportServiceBase(TimeProvider timeProvider) : IAdminErrorReportService
{
    /// <summary>Kısa mesajın en fazla uzunluğu.</summary>
    public const int SummaryLength = 120;

    /// <summary>Varsayılan durum filtresi.</summary>
    public const string OpenFilter = "open";

    /// <summary>Filtresiz değer.</summary>
    public const string AllFilter = "all";

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public async Task<Result<AdminErrorReportPageDto>> ListAsync(AdminErrorReportsRequest? request, CancellationToken cancellationToken)
    {
        var page = AdminRange.ParsePage(request?.Page);
        var (sourceName, source) = ParseSource(request?.Source);
        var (statusName, statuses) = ParseStatusFilter(request?.Status);
        var result = await ListReportsAsync(source, statuses, (page - 1) * AdminRange.PageSize, AdminRange.PageSize, cancellationToken).ConfigureAwait(false);
        var users = await UsersByIdAsync(cancellationToken).ConfigureAwait(false);
        var items = result.Items
            .Select(r =>
            {
                var user = Reporter(users, r.UserId);
                return new AdminErrorReportRowDto(
                    r.Number,
                    r.CreatedAt,
                    ErrorReportServiceBase.SourceName(r.Source),
                    user.Name,
                    user.Email,
                    Summarize(r.Description),
                    ErrorReportRules.StatusName(r.Status));
            })
            .ToList();
        var filtered = sourceName != AllFilter || statusName != OpenFilter;
        return Result<AdminErrorReportPageDto>.Success(new AdminErrorReportPageDto(items, page, AdminRange.PageSize, result.Total, filtered, sourceName, statusName));
    }

    /// <inheritdoc />
    public async Task<Result<AdminErrorReportDetailDto>> GetAsync(long number, CancellationToken cancellationToken)
    {
        var report = await LoadAsync(number, cancellationToken).ConfigureAwait(false);
        return report is null
            ? Result<AdminErrorReportDetailDto>.Failure(MonitoringErrors.ReportNotFound)
            : Result<AdminErrorReportDetailDto>.Success(await DetailAsync(report, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<AdminErrorReportDetailDto>> ChangeStatusAsync(long number, ErrorReportStatusRequest? request, CancellationToken cancellationToken)
    {
        if (ErrorReportRules.ParseStatus(request?.Status) is not { } status)
        {
            return Result<AdminErrorReportDetailDto>.Failure(MonitoringErrors.ReportStatusInvalid);
        }

        var note = string.IsNullOrWhiteSpace(request!.Note) ? null : request.Note.Trim();
        if (note is { Length: > ErrorReportRules.AdminNoteMaxLength })
        {
            return Result<AdminErrorReportDetailDto>.Failure(MonitoringErrors.ReportNoteTooLong);
        }

        var changed = await UpdateStatusAsync(
            number,
            status,
            ErrorReportRules.Clean(note, ErrorReportRules.AdminNoteMaxLength),
            TimeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);
        return changed
            ? await GetAsync(number, cancellationToken).ConfigureAwait(false)
            : Result<AdminErrorReportDetailDto>.Failure(MonitoringErrors.ReportNotFound);
    }

    /// <summary>Bildirimlerin bir sayfasını okur.</summary>
    /// <param name="source">Kaynak; <c>null</c> tümü.</param>
    /// <param name="statuses">Durumlar; <c>null</c> tümü.</param>
    /// <param name="skip">Atlanacak.</param>
    /// <param name="take">Alınacak.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    protected abstract Task<ErrorReportPage> ListReportsAsync(
        ErrorReportSource? source,
        IReadOnlyCollection<ErrorReportStatus>? statuses,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Numarayla bildirimi okur.</summary>
    /// <param name="number">Numara.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim ya da <c>null</c>.</returns>
    protected abstract Task<ErrorReport?> LoadAsync(long number, CancellationToken cancellationToken);

    /// <summary>Durumu ve notu yazar.</summary>
    /// <param name="number">Numara.</param>
    /// <param name="status">Durum.</param>
    /// <param name="note">Not (maskeli).</param>
    /// <param name="at">An.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değiştiyse <c>true</c>.</returns>
    protected abstract Task<bool> UpdateStatusAsync(long number, ErrorReportStatus status, string? note, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Kullanıcı dizinini okur (Identity.Contracts).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    protected abstract Task<IReadOnlyList<UserDirectoryEntry>> ListUsersAsync(CancellationToken cancellationToken);

    private static (string Name, ErrorReportSource? Source) ParseSource(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "WEB" => ("web", ErrorReportSource.Web),
        "AGENT" => ("agent", ErrorReportSource.Agent),
        "API" => ("api", ErrorReportSource.Api),
        _ => (AllFilter, null),
    };

    private static (string Name, IReadOnlyCollection<ErrorReportStatus>? Statuses) ParseStatusFilter(string? value)
    {
        var trimmed = value?.Trim();
        if (string.Equals(trimmed, AllFilter, StringComparison.OrdinalIgnoreCase))
        {
            return (AllFilter, null);
        }

        return ErrorReportRules.ParseStatus(trimmed) is { } status
            ? (ErrorReportRules.StatusName(status), [status])
            : (OpenFilter, ErrorReportRules.OpenStatuses);
    }

    private static string Summarize(string? description)
    {
        var text = (description ?? string.Empty).ReplaceLineEndings(" ").Trim();
        return text.Length > SummaryLength ? text[..SummaryLength] + "…" : text;
    }

    private static (string Name, string Email) Reporter(Dictionary<Guid, UserDirectoryEntry> users, Guid? userId) =>
        userId is { } id && users.TryGetValue(id, out var user)
            ? (user.Name, user.Email)
            : (AdminStatisticsServiceBase.UnknownUser, AdminStatisticsServiceBase.UnknownUser);

    private async Task<Dictionary<Guid, UserDirectoryEntry>> UsersByIdAsync(CancellationToken cancellationToken) =>
        (await ListUsersAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(u => u.Id);

    private async Task<AdminErrorReportDetailDto> DetailAsync(ErrorReport report, CancellationToken cancellationToken)
    {
        var user = Reporter(await UsersByIdAsync(cancellationToken).ConfigureAwait(false), report.UserId);
        return new AdminErrorReportDetailDto(ErrorReportServiceBase.ToDto(report), user.Name, user.Email, report.AdminNote, report.StatusChangedAt);
    }
}
