namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Gecikme hatırlatmasının takvimi (spec 0015 C-7, gereksinim 9; saf hesap). Türkiye saatiyle <see cref="SendHour"/>:00'dan önce
/// hiçbir şey gitmez. Sonrasında, vadeden itibaren 1., 8. ve 15. günde sırasıyla 1., 2. ve 3. hatırlatma vaktidir; o ana kadar
/// vakti gelmiş en büyük sıra, gönderilmiş en büyük sıradan büyükse o sıra gönderilir. Böylece kaçırılan günler birikmez — birkaç
/// gün kapalı kalan sistem açılınca yalnızca bir hatırlatma gider (atlanan sıralar gönderilmez) — ve en fazla
/// <see cref="MaxReminders"/> hatırlatma olur.
/// </summary>
public static class ReminderSchedule
{
    public const int MaxReminders = 3;
    public const int SendHour = 10;
    private static readonly int[] DayOffsets = [1, 8, 15];

    /// <summary>Şimdi (Türkiye saati) gönderilecek hatırlatma sırası; yoksa <c>null</c>.</summary>
    public static int? Due(DateOnly dueDate, DateTimeOffset turkeyNow, int lastSentSequence)
    {
        if (turkeyNow.Hour < SendHour)
        {
            return null;
        }

        DateOnly today = DateOnly.FromDateTime(turkeyNow.DateTime);
        int reached = 0;
        for (int i = 0; i < DayOffsets.Length; i++)
        {
            if (today >= dueDate.AddDays(DayOffsets[i]))
            {
                reached = i + 1;
            }
        }

        return reached > lastSentSequence ? reached : null;
    }
}
