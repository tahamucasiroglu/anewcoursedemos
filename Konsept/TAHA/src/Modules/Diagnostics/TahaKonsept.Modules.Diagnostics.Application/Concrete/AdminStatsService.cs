using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// <see cref="IAdminStatsService"/> uygulaması: kayıtlı tüm <see cref="IStatsContributor"/>'ları sırayla çağırır, sözlükleri birleştirir
/// (aynı anahtarda sonraki kazanır), hata veren katkıcıyı atlayıp <see cref="AdminStatsResponse.FailedContributors"/>'a yazar ve
/// <see cref="StatsKeys"/> anahtarlarını tipli alanlara, diğer basit değerleri <see cref="AdminStatsResponse.Extra"/>'ya koyar.
/// Seriler son <see cref="SeriesDays"/> günü (Türkiye saati) boşluksuz kapsar.
/// </summary>
public sealed class AdminStatsService : DiagnosticsServiceBase, IAdminStatsService
{
    /// <summary>Günlük serilerin gün sayısı.</summary>
    public const int SeriesDays = 30;

    private static readonly HashSet<string> TypedKeys = new(StringComparer.Ordinal)
    {
        StatsKeys.UsersTotal, StatsKeys.ConceptsTotal, StatsKeys.VideosTotal, StatsKeys.VideosPerDay, StatsKeys.SpendPerDayUsd,
        StatsKeys.SpendMonthUsd, StatsKeys.ErrorReportsOpen, StatsKeys.ActivityTotal, StatsKeys.ActivityByLevel,
    };

    private readonly IReadOnlyList<IStatsContributor> _contributors;
    private readonly IClock _clock;
    private readonly ILogger<AdminStatsService> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="contributors">Kayıtlı katkıcılar.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="logger">Teknik günlükçü.</param>
    public AdminStatsService(IEnumerable<IStatsContributor> contributors, IClock clock, ICurrentUser currentUser, ILogger<AdminStatsService> logger)
        : base(currentUser)
    {
        _contributors = (contributors ?? throw new ArgumentNullException(nameof(contributors))).ToList();
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>İstatistikleri hesaplar (yönetici).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstatistikler.</returns>
    public async Task<Result<AdminStatsResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        var merged = new Dictionary<string, object>(StringComparer.Ordinal);
        var failed = new List<string>();
        foreach (var contributor in _contributors)
        {
            try
            {
                var stats = await contributor.GetStatsAsync(cancellationToken);
                foreach (var (key, value) in stats)
                {
                    merged[key] = value;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) // Tek bir modülün hatası paneli düşürmemeli; atlanır ve raporlanır.
            {
                failed.Add(contributor.GetType().Name);
                _logger.LogWarning(ex, "İstatistik katkıcısı hata verdi, atlandı: {Contributor}", contributor.GetType().FullName);
            }
        }

        return Build(merged, failed, _clock.UtcNow);
    }

    /// <summary>Birleşik sözlükten yanıtı kurar (test edilebilir saf fonksiyon).</summary>
    /// <param name="merged">Birleşik istatistikler.</param>
    /// <param name="failed">Atlanan katkıcılar.</param>
    /// <param name="nowUtc">Şu an.</param>
    /// <returns>Yanıt.</returns>
    internal static AdminStatsResponse Build(IReadOnlyDictionary<string, object> merged, IReadOnlyList<string> failed, DateTimeOffset nowUtc)
    {
        var days = DiagnosticsTime.LastDays(nowUtc, SeriesDays);
        return new AdminStatsResponse
        {
            UserCount = StatsValueReader.ReadInt(merged, StatsKeys.UsersTotal),
            TotalConcepts = StatsValueReader.ReadInt(merged, StatsKeys.ConceptsTotal),
            TotalVideos = StatsValueReader.ReadInt(merged, StatsKeys.VideosTotal),
            OpenErrorReports = StatsValueReader.ReadInt(merged, StatsKeys.ErrorReportsOpen),
            TotalActivityLogs = StatsValueReader.ReadInt(merged, StatsKeys.ActivityTotal),
            MonthSpendUsd = StatsValueReader.ReadNumber(merged, StatsKeys.SpendMonthUsd),
            VideosPerDay = StatsValueReader.ReadDailySeries(merged.GetValueOrDefault(StatsKeys.VideosPerDay), days),
            SpendPerDayUsd = StatsValueReader.ReadDailySeries(merged.GetValueOrDefault(StatsKeys.SpendPerDayUsd), days),
            ActivityByLevel = StatsValueReader.ReadLevelCounts(merged.GetValueOrDefault(StatsKeys.ActivityByLevel)),
            Extra = merged
                .Where(p => !TypedKeys.Contains(p.Key) && StatsValueReader.IsScalar(p.Value))
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            FailedContributors = failed,
            GeneratedAt = nowUtc,
        };
    }
}
