using System.Globalization;
using System.Text.RegularExpressions;
using Konsept.Shared.Logging.Base;

namespace Konsept.Shared.Logging.Concrete;

/// <summary>
/// Dosya adındaki tarihe göre (<c>önek-yyyyMMdd.log</c>, <c>önek-yyyyMMdd_001.log</c>) saklama süresinden eski log
/// dosyalarını siler. Serilog'un dosya sayısı sınırı log yazılmayan günleri saymadığı için süre buradan uygulanır
/// (plan 0008, Risk 3). Varsayılan: 7 günlük dosya korunur, 8 günlük silinir (UTC gün).
/// </summary>
public sealed partial class LogFileJanitor : LogFileJanitorBase
{
    /// <summary>Varsayılan saklama süresi (gün).</summary>
    public const int DefaultRetentionDays = 7;

    /// <summary>Initializes a new instance of the <see cref="LogFileJanitor"/> class.</summary>
    /// <param name="timeProvider">Zaman kaynağı.</param>
    /// <param name="retentionDays">Kaç günlük dosya korunur.</param>
    public LogFileJanitor(TimeProvider timeProvider, int retentionDays = DefaultRetentionDays)
        : base(timeProvider, retentionDays)
    {
    }

    /// <inheritdoc />
    public override int Clean(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
        var deleted = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*.log"))
        {
            var match = DatedName().Match(Path.GetFileName(file));
            if (!match.Success
                || !DateOnly.TryParseExact(match.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || today.DayNumber - date.DayNumber <= RetentionDays)
            {
                continue;
            }

            try
            {
                File.Delete(file);
                deleted++;
            }
            catch (IOException)
            {
                // Dosya o an açık (başka süreç yazıyor) — bir sonraki temizlikte denenir.
            }
            catch (UnauthorizedAccessException)
            {
                // Yetki yok — bir sonraki temizlikte denenir.
            }
        }

        return deleted;
    }

    [GeneratedRegex(@"-(?<date>\d{8})(_\d+)?\.log$", RegexOptions.CultureInvariant)]
    private static partial Regex DatedName();
}
