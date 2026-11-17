using System.Globalization;

namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Admin kullanıcı listesinin araması ve durum filtresi (spec 0024 Req 2): ad ya da e-postada büyük/küçük harf duyarsız "içerir",
/// en az 2 karakter (daha kısası uygulanmaz); tr-TR (İ/ı) ya da kültürden bağımsız eşleşme yeterlidir (0010 kuralıyla aynı).
/// </summary>
public static class UserSearch
{
    /// <summary>Sayfa boyutu.</summary>
    public const int PageSize = 25;

    /// <summary>Aramanın uygulanacağı en kısa uzunluk.</summary>
    public const int MinSearchLength = 2;

    /// <summary>Durum filtresi: tümü.</summary>
    public const string StatusAll = "all";

    /// <summary>Durum filtresi: aktif.</summary>
    public const string StatusActive = "active";

    /// <summary>Durum filtresi: banlı.</summary>
    public const string StatusBanned = "banned";

    /// <summary>Aramayı kırpar; 2 karakterden kısaysa <c>null</c> (uygulanmaz).</summary>
    /// <param name="value">Ham arama.</param>
    /// <returns>Arama ya da <c>null</c>.</returns>
    public static string? ParseSearch(string? value)
    {
        var text = value?.Trim();
        return text is { Length: >= MinSearchLength } ? text : null;
    }

    /// <summary>Durum filtresini çözer; bilinmeyen değer tümü sayılır.</summary>
    /// <param name="value">Ham değer.</param>
    /// <returns><see cref="StatusAll"/>, <see cref="StatusActive"/> ya da <see cref="StatusBanned"/>.</returns>
    public static string ParseStatus(string? value) => value switch
    {
        StatusActive => StatusActive,
        StatusBanned => StatusBanned,
        _ => StatusAll,
    };

    /// <summary>Sayfa numarası: 1…1.000.000, geçersiz → 1.</summary>
    /// <param name="value">Ham sayfa.</param>
    /// <returns>Sayfa.</returns>
    public static int ParsePage(int? value) => value is >= 1 and <= 1_000_000 ? value.Value : 1;

    /// <summary>Ad ya da e-posta aramayı içeriyor mu.</summary>
    /// <param name="search">Arama (en az 2 karakter).</param>
    /// <param name="name">Ad.</param>
    /// <param name="email">E-posta.</param>
    /// <returns>Eşleşirse <c>true</c>.</returns>
    public static bool Matches(string search, string name, string email)
    {
        var compare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        bool Has(string? text) =>
            compare.IndexOf(text ?? string.Empty, search, CompareOptions.IgnoreCase) >= 0
            || (text ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase);
        return Has(name) || Has(email);
    }

    /// <summary>Durum filtresine uyuyor mu.</summary>
    /// <param name="status">Çözülmüş filtre.</param>
    /// <param name="isBanned">Kullanıcı banlı mı.</param>
    /// <returns>Uyarsa <c>true</c>.</returns>
    public static bool MatchesStatus(string status, bool isBanned) => status switch
    {
        StatusActive => !isBanned,
        StatusBanned => isBanned,
        _ => true,
    };
}
