using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// Diagnostics'in kendi istatistik katkısı: açık hata bildirimi, toplam günlük kaydı, son 30 günde seviyeye göre günlük kaydı ve
/// (kullanıcı dizini bağlıysa) kullanıcı sayısı. İlk kaydedilen katkıcıdır; Identity aynı anahtarı verirse onunki kazanır.
/// </summary>
public sealed class DiagnosticsStatsContributor : StatsContributorBase
{
    private readonly IActivityLogRepository _logs;
    private readonly IErrorReportRepository _reports;
    private readonly IAdminUserDirectory _users;
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="logs">Günlük deposu.</param>
    /// <param name="reports">Bildirim deposu.</param>
    /// <param name="users">Kullanıcı dizini.</param>
    /// <param name="clock">Saat.</param>
    public DiagnosticsStatsContributor(IActivityLogRepository logs, IErrorReportRepository reports, IAdminUserDirectory users, IClock clock)
    {
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Değerleri yazar.</summary>
    /// <param name="stats">Sözlük.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ContributeAsync(IDictionary<string, object> stats, CancellationToken cancellationToken)
    {
        stats[StatsKeys.ErrorReportsOpen] = await _reports.CountAsync(r => r.Status != ErrorReportStatus.Resolved, cancellationToken);
        stats[StatsKeys.ActivityTotal] = await _logs.CountAsync(null, cancellationToken);
        var since = DiagnosticsTime.DayRangeUtc(DiagnosticsTime.LastDays(_clock.UtcNow, AdminStatsService.SeriesDays)[0]).FromUtc;
        stats[StatsKeys.ActivityByLevel] = await _logs.CountByLevelAsync(since, cancellationToken);
        if (_users.IsAvailable)
        {
            stats[StatsKeys.UsersTotal] = (await _users.ListAsync(cancellationToken)).Count;
        }
    }
}
