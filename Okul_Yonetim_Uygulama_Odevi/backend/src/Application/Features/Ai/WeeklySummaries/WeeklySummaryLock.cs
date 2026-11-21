using System.Globalization;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Öğrenci-hafta sırasının anahtarı (spec 0014 AC-7): taslak kaydı/yeniden oluşturma, "Vazgeç" (Ai) ve gönderim (Communication)
/// aynı anahtarı alır — eşzamanlı iki gönderimde ikincisi ilkinin onayını görür ("zaten gönderildi"), tek özet gider.
/// </summary>
public static class WeeklySummaryLock
{
    public static string Key(Guid studentId, DateOnly weekStart) =>
        "weekly-summary:" + studentId.ToString("N") + ":" + weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
