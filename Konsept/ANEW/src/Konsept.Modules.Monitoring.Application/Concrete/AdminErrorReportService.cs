using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>Admin Hata bildirimleri: bildirim deposu ve kullanıcı dizini (Identity.Contracts) üzerinden.</summary>
/// <param name="reports">Bildirim deposu.</param>
/// <param name="users">Kullanıcı dizini.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class AdminErrorReportService(IErrorReportRepository reports, IUserDirectory users, TimeProvider timeProvider)
    : AdminErrorReportServiceBase(timeProvider)
{
    /// <inheritdoc />
    protected override Task<ErrorReportPage> ListReportsAsync(
        ErrorReportSource? source,
        IReadOnlyCollection<ErrorReportStatus>? statuses,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        reports.ListAsync(source, statuses, skip, take, cancellationToken);

    /// <inheritdoc />
    protected override Task<ErrorReport?> LoadAsync(long number, CancellationToken cancellationToken) => reports.GetByNumberAsync(number, cancellationToken);

    /// <inheritdoc />
    protected override Task<bool> UpdateStatusAsync(long number, ErrorReportStatus status, string? note, DateTimeOffset at, CancellationToken cancellationToken) =>
        reports.UpdateStatusAsync(number, status, note, at, cancellationToken);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<UserDirectoryEntry>> ListUsersAsync(CancellationToken cancellationToken) => users.ListAsync(cancellationToken);
}
