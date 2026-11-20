using Microsoft.Extensions.Logging;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization;

/// <summary>
/// Okulun takvim günü ve güncel dönem seçimi (spec 0008 gereksinim 5). "Bugün" Türkiye saatiyle hesaplanır
/// (<c>Europe/Istanbul</c>; saat dilimi bulunamazsa sabit UTC+3 — Türkiye 2016'dan beri yaz saati uygulamıyor — ve bir
/// Warning logu). Güncel dönem bugünü kapsayan dönemdir (uç günler dahil); yoksa henüz başlamamış en yakın dönem
/// ("yaklaşan"); o da yoksa yok. Yönetim listesi, okul takvimi ve sonraki spec'ler (yoklama) aynı kuralı kullanır.
/// </summary>
public sealed class SchoolCalendar
{
    private static readonly TimeSpan FixedTurkeyOffset = TimeSpan.FromHours(3);
    private readonly TimeProvider timeProvider;
    private readonly TimeZoneInfo? zone;

    public SchoolCalendar(TimeProvider timeProvider, ILogger<SchoolCalendar> logger)
    {
        this.timeProvider = timeProvider;
        try
        {
            this.zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning("Europe/Istanbul saat dilimi bulunamadı; okul günü sabit UTC+3 ile hesaplanıyor");
            this.zone = null;
        }
    }

    /// <summary>Verilen andaki Türkiye günü.</summary>
    public static DateOnly TurkeyDay(DateTimeOffset instant, TimeZoneInfo? zone)
    {
        DateTimeOffset local = zone is null ? instant.ToOffset(FixedTurkeyOffset) : TimeZoneInfo.ConvertTime(instant, zone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    /// <summary>Bugünü kapsayan dönem; yoksa henüz başlamamış en yakın dönem; o da yoksa <c>null</c>.</summary>
    public static Term? PickCurrent(IEnumerable<Term> terms, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(terms);
        List<Term> all = [.. terms];
        return all.FirstOrDefault(t => t.Contains(today))
            ?? all.Where(t => t.StartDate > today).OrderBy(t => t.StartDate).FirstOrDefault();
    }

    /// <summary>Listelerin varsayılan dönemi: güncel (ya da yaklaşan) dönem, yoksa en yeni dönem.</summary>
    public static Term? PickDefault(IEnumerable<Term> terms, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(terms);
        List<Term> all = [.. terms];
        return PickCurrent(all, today) ?? all.OrderByDescending(t => t.StartDate).FirstOrDefault();
    }

    /// <summary>Okulun bugünü (Türkiye günü).</summary>
    public DateOnly Today() => TurkeyDay(this.timeProvider.GetUtcNow(), this.zone);
}
