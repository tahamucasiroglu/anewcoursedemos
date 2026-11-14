using System.Text;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>Sunucudan gelen adları güvenli yerel dosya adına çevirir (yol ayraçları ve geçersiz karakterler atılır).</summary>
public static class FileNameSanitizer
{
    /// <summary>Adı temizler.</summary>
    /// <param name="name">Ad.</param>
    /// <param name="fallback">Boş kalırsa kullanılacak ad.</param>
    /// <returns>Güvenli dosya adı.</returns>
    public static string Sanitize(string? name, string fallback = "dosya")
    {
        var candidate = Path.GetFileName((name ?? string.Empty).Replace('\\', '/').Split('/').LastOrDefault() ?? string.Empty);
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(candidate.Length);
        foreach (var c in candidate)
        {
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        var cleaned = builder.ToString().Trim().Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }
}
