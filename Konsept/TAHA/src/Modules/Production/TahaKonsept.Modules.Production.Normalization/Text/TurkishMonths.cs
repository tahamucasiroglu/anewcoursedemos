namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>Türkçe ay adları (1 → Ocak … 12 → Aralık) ve tarih kuralının kullandığı regex alternatifi.</summary>
public static class TurkishMonths
{
    /// <summary>Ay adları; indeks 0 boş.</summary>
    public static IReadOnlyList<string> Names { get; } =
        ["", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    /// <summary>Regex alternatifi (ör. <c>Ocak|Şubat|…</c>).</summary>
    public const string Pattern = "Ocak|Şubat|Mart|Nisan|Mayıs|Haziran|Temmuz|Ağustos|Eylül|Ekim|Kasım|Aralık";
}
