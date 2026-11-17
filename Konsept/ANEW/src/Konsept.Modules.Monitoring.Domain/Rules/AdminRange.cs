using System.Globalization;

namespace Konsept.Modules.Monitoring.Domain.Rules;

/// <summary>
/// Admin paneli aralık ve filtre kuralları (spec 0010 Req 4, 7, 8): grafik aralığı 7 / 30 / 90 gün (başka değer → 30), günler
/// Europe/Istanbul'a göre (<see cref="JournalDay"/>), listeler 25'erli; kullanıcı araması en az 2 karakterle uygulanır.
/// </summary>
public static class AdminRange
{
    /// <summary>Varsayılan grafik aralığı (gün).</summary>
    public const int DefaultDays = 30;

    /// <summary>Panel listelerinde sayfa başına kayıt (0024 ve 0025 aynı değeri kullanır).</summary>
    public const int PageSize = 25;

    /// <summary>Kullanıcı aramasının uygulanması için en az karakter.</summary>
    public const int MinSearchLength = 2;

    /// <summary>İstenebilecek en büyük sayfa (taşma koruması).</summary>
    public const int MaxPage = 1_000_000;

    /// <summary>Sayfa numarasını 1…<see cref="MaxPage"/> aralığına çeker.</summary>
    /// <param name="page">İstenen sayfa.</param>
    /// <returns>Sayfa.</returns>
    public static int ParsePage(int? page) => Math.Clamp(page ?? 1, 1, MaxPage);

    /// <summary>İzin verilen aralığa çevirir.</summary>
    /// <param name="days">İstenen gün sayısı.</param>
    /// <returns>7, 30 ya da 90.</returns>
    public static int ParseDays(int? days) => days is 7 or 30 or 90 ? days.Value : DefaultDays;

    /// <summary>Bugünle biten aralığın günleri (eskiden yeniye).</summary>
    /// <param name="today">Bugün (Europe/Istanbul).</param>
    /// <param name="days">Gün sayısı.</param>
    /// <returns>Günler.</returns>
    public static IReadOnlyList<DateOnly> DaysEndingAt(DateOnly today, int days) =>
        [.. Enumerable.Range(0, days).Select(i => today.AddDays(i - days + 1))];

    /// <summary>Tarih metnini (<see cref="JournalDay.DateFormat"/>) çözer.</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Gün ya da geçersiz/boşsa <c>null</c>.</returns>
    public static DateOnly? ParseDate(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && DateOnly.TryParseExact(value.Trim(), JournalDay.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            && day.Year is >= 2000 and <= 9998
            ? day
            : null; // Aralık dışı gün (ör. 9999-12-31) geçersiz sayılır: gün sınırı hesabı taşmaz.

    /// <summary>Kullanıcı aramasını çözer.</summary>
    /// <param name="value">Arama metni.</param>
    /// <returns>Kırpılmış metin; 2 karakterden kısaysa <c>null</c> (filtre uygulanmaz).</returns>
    public static string? ParseSearch(string? value)
    {
        var text = value?.Trim();
        return text is { Length: >= MinSearchLength } ? text : null;
    }

    /// <summary>
    /// Ad ya da e-posta aramayı içeriyor mu (büyük/küçük harf duyarsız): tr-TR (İ/ı) ya da kültürden bağımsız eşleşme yeterlidir —
    /// tr-TR'de "I" ile "i" eşleşmediği için ASCII e-postalar ("VELI" → "veli@") ikinci yoldan bulunur.
    /// </summary>
    /// <param name="search">Arama.</param>
    /// <param name="name">Ad.</param>
    /// <param name="email">E-posta.</param>
    /// <returns>Eşleşirse <c>true</c>.</returns>
    public static bool Matches(string search, string name, string email)
    {
        var compare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        const CompareOptions Options = CompareOptions.IgnoreCase;
        bool Has(string? text) =>
            compare.IndexOf(text ?? string.Empty, search, Options) >= 0
            || (text ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase);
        return Has(name) || Has(email);
    }
}
