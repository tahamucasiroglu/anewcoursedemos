using System.Globalization;
using Konsept.Modules.Identity.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Domain.Rules;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Admin İstatistikler ve Aktiviteler mantığı (spec 0010 Req 3–8). Günler Europe/Istanbul'a göre ayrılır, veri olmayan gün 0'dır;
/// aktif kullanıcı = o gün en az bir kullanıcılı olayı olan kullanıcı. Aktivite satırı yalnızca zaman + ad/e-posta + tür etiketi
/// taşır (BR-1). Veri kaynakları (Identity, Production, Monitoring) alt sınıftan gelir.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class AdminStatisticsServiceBase(TimeProvider timeProvider) : IAdminStatisticsService
{
    /// <summary>Kullanıcısı bulunamayan aktivite satırında ad/e-posta yerine gösterilen metin.</summary>
    public const string UnknownUser = "—";

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public async Task<Result<AdminStatisticsDto>> GetStatisticsAsync(int? days, CancellationToken cancellationToken)
    {
        var range = AdminRange.ParseDays(days);
        var today = JournalDay.DayOf(TimeProvider.GetUtcNow());
        var dayList = AdminRange.DaysEndingAt(today, range);
        var fromUtc = JournalDay.RangeOf(dayList[0]).FromUtc;
        var toUtc = JournalDay.RangeOf(today).ToUtc;

        var users = await ListUsersAsync(cancellationToken).ConfigureAwait(false);
        var clips = await ListFirstCompletionsAsync(cancellationToken).ConfigureAwait(false);
        var openReports = await CountOpenErrorReportsAsync(cancellationToken).ConfigureAwait(false);
        var events = await ListUserEventTimesAsync(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);

        var registrations = CountByDay(users.Select(u => u.RegisteredAt), fromUtc, toUtc);
        var clipDays = CountByDay(clips, fromUtc, toUtc);
        var active = events
            .Select(e => (e.UserId, Day: JournalDay.DayOf(e.OccurredAt)))
            .Distinct()
            .GroupBy(p => p.Day)
            .ToDictionary(g => g.Key, g => g.Count());

        var series = dayList
            .Select(d => new AdminDayDto(
                d.ToString(JournalDay.DateFormat, CultureInfo.InvariantCulture),
                registrations.GetValueOrDefault(d),
                clipDays.GetValueOrDefault(d),
                active.GetValueOrDefault(d)))
            .ToList();
        return Result<AdminStatisticsDto>.Success(new AdminStatisticsDto(users.Count, clips.Count, openReports, range, series, users.Count(u => u.IsBanned)));
    }

    /// <inheritdoc />
    public async Task<Result<AdminActivityPageDto>> GetActivitiesAsync(AdminActivitiesRequest? request, CancellationToken cancellationToken)
    {
        var page = AdminRange.ParsePage(request?.Page);
        var search = AdminRange.ParseSearch(request?.User);
        var from = AdminRange.ParseDate(request?.From);
        var to = AdminRange.ParseDate(request?.To);
        var dateIgnored = from is { } f && to is { } t && f > t;
        if (dateIgnored)
        {
            (from, to) = (null, null); // Başlangıç bitişten sonra: tarih filtresi uygulanmaz (Req 7).
        }

        var users = await ListUsersAsync(cancellationToken).ConfigureAwait(false);
        var byId = users.ToDictionary(u => u.Id);
        IReadOnlyCollection<Guid>? userIds = search is null
            ? null
            : [.. users.Where(u => AdminRange.Matches(search, u.Name, u.Email)).Select(u => u.Id)];
        var fromUtc = from is { } start ? JournalDay.RangeOf(start).FromUtc : (DateTimeOffset?)null;
        var toUtc = to is { } end ? JournalDay.RangeOf(end).ToUtc : (DateTimeOffset?)null;

        var result = userIds is { Count: 0 }
            ? new ActivityPage([], 0)
            : await ListActivitiesAsync(userIds, fromUtc, toUtc, (page - 1) * AdminRange.PageSize, AdminRange.PageSize, cancellationToken).ConfigureAwait(false);
        var items = result.Items
            .Select(e =>
            {
                var user = e.UserId is { } id ? byId.GetValueOrDefault(id) : null;
                return new AdminActivityDto(
                    e.OccurredAt,
                    user?.Name ?? UnknownUser,
                    user?.Email ?? UnknownUser,
                    ActivityKinds.Describe(e.Type, e.Level, e.EndedAt, e.Message));
            })
            .ToList();
        var filtered = search is not null || fromUtc is not null || toUtc is not null;
        return Result<AdminActivityPageDto>.Success(new AdminActivityPageDto(items, page, AdminRange.PageSize, result.Total, filtered, dateIgnored));
    }

    /// <summary>Kullanıcı dizinini okur (Identity.Contracts).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    protected abstract Task<IReadOnlyList<UserDirectoryEntry>> ListUsersAsync(CancellationToken cancellationToken);

    /// <summary>Kliplerin ilk tamamlanma anlarını okur (Production, Monitoring.Contracts üzerinden).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anlar.</returns>
    protected abstract Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken);

    /// <summary>Açık hata bildirimlerini sayar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    protected abstract Task<int> CountOpenErrorReportsAsync(CancellationToken cancellationToken);

    /// <summary>Aralıktaki kullanıcılı olayları okur.</summary>
    /// <param name="fromUtc">Başlangıç.</param>
    /// <param name="toUtc">Bitiş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Çiftler.</returns>
    protected abstract Task<IReadOnlyList<UserEventTime>> ListUserEventTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    /// <summary>Kullanıcılı olayların sayfasını okur.</summary>
    /// <param name="userIds">Kullanıcı kümesi.</param>
    /// <param name="fromUtc">Başlangıç.</param>
    /// <param name="toUtc">Bitiş.</param>
    /// <param name="skip">Atlanacak.</param>
    /// <param name="take">Alınacak.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    protected abstract Task<ActivityPage> ListActivitiesAsync(
        IReadOnlyCollection<Guid>? userIds,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken);

    private static Dictionary<DateOnly, int> CountByDay(IEnumerable<DateTimeOffset> instants, DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
        instants
            .Where(i => i >= fromUtc && i < toUtc)
            .GroupBy(JournalDay.DayOf)
            .ToDictionary(g => g.Key, g => g.Count());
}
