namespace OkulYonetim.Domain.Appointments;

/// <summary>
/// Müsaitlik aralığını görüşme dilimlerine bölen saf hesap (spec 0016 C-1, gereksinim 1). Kurallar: gün bugün ile
/// <see cref="MaxDaysAhead"/> gün sonrası arasında (Türkiye günü); başlangıç ve bitiş aynı gün, <see cref="StepMinutes"/> dakikalık
/// adımda, başlangıç bitişten önce, aralık en fazla <see cref="MaxRangeHours"/> saat; süre 15, 20 ya da 30 dakika. Dilimler
/// başlangıçtan süre adımıyla dizilir; sona kalan kısa parça dilim olmaz; başlamış (başlangıcı şu an ya da önce) dilim atılır.
/// Türkiye saatinden UTC'ye çeviri yalnızca burada yapılır (<see cref="ToInstant"/>; saat dilimi bulunamazsa sabit UTC+3 —
/// Türkiye 2016'dan beri yaz saati uygulamıyor).
/// </summary>
public static class AvailabilityPlanner
{
    public const int StepMinutes = 15;
    public const int MaxDaysAhead = 60;
    public const int MaxRangeHours = 8;

    private static readonly int[] Durations = [15, 20, 30];
    private static readonly TimeSpan FixedTurkeyOffset = TimeSpan.FromHours(3);

    public static IReadOnlyList<int> AllowedDurations => Durations;

    /// <summary>Gün bugün ile 60 gün sonrası arasında mı (iki uç dahil).</summary>
    public static bool IsDayAllowed(DateOnly day, DateOnly today) => day >= today && day <= today.AddDays(MaxDaysAhead);

    /// <summary>Saat 15 dakikalık adımda ve saniyesiz mi.</summary>
    public static bool IsOnStep(TimeOnly time) => time.Second == 0 && time.Millisecond == 0 && time.Minute % StepMinutes == 0;

    public static bool IsDurationAllowed(int minutes) => Durations.Contains(minutes);

    /// <summary>Aralık en fazla 8 saat mi (başlangıç bitişten önce olmalı).</summary>
    public static bool IsRangeAllowed(TimeOnly start, TimeOnly end) => start < end && (end - start) <= TimeSpan.FromHours(MaxRangeHours);

    /// <summary>Türkiye saatiyle gün + saatin UTC anı.</summary>
    public static DateTimeOffset ToInstant(DateOnly day, TimeOnly time, TimeZoneInfo? zone)
    {
        DateTime local = day.ToDateTime(time, DateTimeKind.Unspecified);
        TimeSpan offset = zone is null ? FixedTurkeyOffset : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    /// <summary>Aralığın başlamamış dilimleri (kurallara uymayan girişte <see cref="ArgumentOutOfRangeException"/>).</summary>
    public static IReadOnlyList<SlotWindow> Plan(DateOnly day, TimeOnly start, TimeOnly end, int minutes, DateOnly today, TimeZoneInfo? zone, DateTimeOffset now)
    {
        if (!IsDayAllowed(day, today) || !IsOnStep(start) || !IsOnStep(end) || !IsRangeAllowed(start, end) || !IsDurationAllowed(minutes))
        {
            throw new ArgumentOutOfRangeException(nameof(day), "Müsaitlik aralığı geçersiz.");
        }

        TimeSpan length = TimeSpan.FromMinutes(minutes);
        List<SlotWindow> slots = [];
        for (TimeSpan at = start.ToTimeSpan(); at + length <= end.ToTimeSpan(); at += length)
        {
            DateTimeOffset startsAt = ToInstant(day, TimeOnly.FromTimeSpan(at), zone);
            if (startsAt > now)
            {
                slots.Add(new SlotWindow(startsAt, startsAt + length));
            }
        }

        return slots;
    }
}
