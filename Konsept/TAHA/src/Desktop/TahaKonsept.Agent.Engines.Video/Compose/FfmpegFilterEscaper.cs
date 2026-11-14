namespace TahaKonsept.Agent.Engines.Video.Compose;

/// <summary>
/// ffmpeg filtre grafiği kaçış kuralları (ffmpeg-filters "Notes on filtergraph escaping"). Bir değer iki düzeyde kaçırılır:
/// (1) filtre seçeneği düzeyi: <c>\ ' :</c>; (2) filtre grafiği düzeyi: <c>\ ' [ ] , ;</c>.
/// Windows yolları önce ileri bölüye çevrilir (<c>C:\a b\x.ass</c> → <c>C\\\:/a b/x.ass</c>). Argüman kabuksuz verildiği için
/// başka kaçış gerekmez. Önemli üyeler: <see cref="EscapeOptionValue"/>, <see cref="EscapePath"/>.
/// </summary>
public static class FfmpegFilterEscaper
{
    /// <summary>Bir filtre seçeneği değerini iki düzeyde kaçırır.</summary>
    /// <param name="value">Ham değer.</param>
    /// <returns>Filtre grafiğine doğrudan yazılabilir değer.</returns>
    public static string EscapeOptionValue(string value)
    {
        var level1 = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal);

        return level1
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }

    /// <summary>Dosya yolunu filtre seçeneği olarak kaçırır (ters bölüler ileri bölüye çevrilir).</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>Kaçırılmış yol.</returns>
    public static string EscapePath(string path) => EscapeOptionValue(Path.GetFullPath(path).Replace('\\', '/'));

    /// <summary>concat demuxer liste dosyası satırı (<c>file '...'</c>, tek tırnak kaçışlı).</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>Satır.</returns>
    public static string ConcatListLine(string path) =>
        "file '" + Path.GetFullPath(path).Replace('\\', '/').Replace("'", "'\\''", StringComparison.Ordinal) + "'";
}
