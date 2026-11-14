using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Yönetim paneli için Production katkıcısı (Diagnostics <c>IStatsContributor</c>): toplam video (<see cref="StatsKeys.VideosTotal"/>)
/// ve son <see cref="SeriesDays"/> günün günlük video sayısı (<see cref="StatsKeys.VideosPerDay"/>, Türkiye saatine göre gün → adet; boş günler 0).
/// </summary>
public sealed class ProductionStatsContributor : StatsContributorBase
{
    /// <summary>Günlük serinin gün sayısı.</summary>
    public const int SeriesDays = 30;

    private readonly IVideoRepository _videos;
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="videos">Video deposu.</param>
    /// <param name="clock">Saat.</param>
    public ProductionStatsContributor(IVideoRepository videos, IClock clock)
    {
        _videos = videos ?? throw new ArgumentNullException(nameof(videos));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Video değerlerini sözlüğe yazar.</summary>
    /// <param name="stats">Sözlük.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ContributeAsync(IDictionary<string, object> stats, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var today = TurkeyTime.DayOf(_clock.UtcNow);
        var first = today.AddDays(-(SeriesDays - 1));
        var created = await _videos.ListCreatedSinceAsync(TurkeyTime.StartOfDayUtc(first), cancellationToken);
        var perDay = Enumerable.Range(0, SeriesDays).ToDictionary(i => first.AddDays(i), _ => 0);
        foreach (var day in created.Select(TurkeyTime.DayOf))
        {
            if (perDay.ContainsKey(day))
            {
                perDay[day]++;
            }
        }

        stats[StatsKeys.VideosTotal] = await _videos.CountAsync(cancellationToken: cancellationToken);
        stats[StatsKeys.VideosPerDay] = perDay;
    }
}
