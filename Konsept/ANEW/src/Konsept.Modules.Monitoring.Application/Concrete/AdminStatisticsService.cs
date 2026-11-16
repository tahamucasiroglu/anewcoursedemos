using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Contracts.Abstract;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>Admin istatistikleri: kullanıcı dizini (Identity.Contracts), klip kaynağı (Production) ve Monitoring'in kendi sorguları.</summary>
/// <param name="users">Kullanıcı dizini.</param>
/// <param name="clips">Klip istatistiği.</param>
/// <param name="activities">Admin sorguları.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class AdminStatisticsService(
    IUserDirectory users,
    IClipStatistics clips,
    IAdminActivityRepository activities,
    TimeProvider timeProvider) : AdminStatisticsServiceBase(timeProvider)
{
    /// <inheritdoc />
    protected override Task<IReadOnlyList<UserDirectoryEntry>> ListUsersAsync(CancellationToken cancellationToken) => users.ListAsync(cancellationToken);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken) =>
        clips.ListFirstCompletionsAsync(cancellationToken);

    /// <inheritdoc />
    protected override Task<int> CountOpenErrorReportsAsync(CancellationToken cancellationToken) => activities.CountOpenErrorReportsAsync(cancellationToken);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<UserEventTime>> ListUserEventTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        activities.ListUserEventTimesAsync(fromUtc, toUtc, cancellationToken);

    /// <inheritdoc />
    protected override Task<ActivityPage> ListActivitiesAsync(
        IReadOnlyCollection<Guid>? userIds,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        activities.ListActivitiesAsync(userIds, fromUtc, toUtc, skip, take, cancellationToken);
}
