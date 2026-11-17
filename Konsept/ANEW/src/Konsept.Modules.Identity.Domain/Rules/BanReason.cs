namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>Ban sebebi kuralı (spec 0024 Req 3): isteğe bağlı, en fazla 500 karakter; yalnızca admin görür.</summary>
public static class BanReason
{
    /// <summary>En fazla uzunluk.</summary>
    public const int MaxLength = 500;

    /// <summary>Sebebi kırpar; boşsa <c>null</c>.</summary>
    /// <param name="reason">Ham sebep.</param>
    /// <returns>Kırpılmış sebep ya da <c>null</c>.</returns>
    public static string? Normalize(string? reason) => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    /// <summary>Sebep sınırı aşıyor mu (kırpıldıktan sonra).</summary>
    /// <param name="reason">Ham sebep.</param>
    /// <returns>Aşıyorsa <c>true</c>.</returns>
    public static bool IsTooLong(string? reason) => (Normalize(reason)?.Length ?? 0) > MaxLength;
}
