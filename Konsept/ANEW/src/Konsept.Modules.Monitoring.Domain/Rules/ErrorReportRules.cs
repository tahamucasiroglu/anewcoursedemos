using Konsept.Shared.Kernel.Security;

namespace Konsept.Modules.Monitoring.Domain.Rules;

/// <summary>
/// Hata bildirimi kuralları (spec 0022 Req 1, 4, 15): açıklama 10–4000 karakter; kullanıcı başına bir saatte en fazla 10 bildirim
/// (web + ajan; API'nin otomatik kayıtları sayılmaz); ekli metin alanlarının sınırları. Her metin maskelenip kırpılır (H-2).
/// </summary>
public static class ErrorReportRules
{
    /// <summary>Açıklamanın en az uzunluğu.</summary>
    public const int DescriptionMinLength = 10;

    /// <summary>Açıklamanın en fazla uzunluğu.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Bir saatte kabul edilen en fazla bildirim (web + ajan).</summary>
    public const int HourlyLimit = 10;

    /// <summary>Web bildirimine eklenen son Günlük kaydı sayısı.</summary>
    public const int JournalEntryCount = 20;

    /// <summary>Kısa alanların (sayfa, korelasyon, versiyon, işletim sistemi, bileşen, adım) en fazla uzunluğu.</summary>
    public const int ShortFieldMaxLength = 200;

    /// <summary>Hata özetinin en fazla uzunluğu.</summary>
    public const int SummaryMaxLength = 4000;

    /// <summary>Ekli satırların (Günlük ya da ajan logu) toplam en fazla uzunluğu.</summary>
    public const int AttachmentMaxLength = 64 * 1024;

    /// <summary>Ajan bildirimindeki en fazla log satırı.</summary>
    public const int MaxLogLines = 200;

    /// <summary>Hız sınırı penceresi.</summary>
    public static readonly TimeSpan RateWindow = TimeSpan.FromHours(1);

    /// <summary>Metni maskeler ve sınıra kırpar; boşsa <c>null</c>.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="maxLength">En fazla uzunluk.</param>
    /// <returns>Temiz metin ya da <c>null</c>.</returns>
    public static string? Clean(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var masked = SecretMasker.Mask(text.Trim());
        return masked.Length > maxLength ? masked[..maxLength] : masked;
    }
}
