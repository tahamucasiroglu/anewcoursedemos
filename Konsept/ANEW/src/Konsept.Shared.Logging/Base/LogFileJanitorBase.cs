using Konsept.Shared.Logging.Abstract;

namespace Konsept.Shared.Logging.Base;

/// <summary>Log dosyası temizliğinin ortak temeli: zaman kaynağı ve saklama süresi.</summary>
/// <param name="timeProvider">Zaman kaynağı.</param>
/// <param name="retentionDays">Kaç günlük dosya korunur (bugün 0. gündür).</param>
public abstract class LogFileJanitorBase(TimeProvider timeProvider, int retentionDays) : ILogFileJanitor
{
    /// <summary>Gets zaman kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider;

    /// <summary>Gets saklama süresi (gün).</summary>
    protected int RetentionDays { get; } = retentionDays;

    /// <inheritdoc />
    public abstract int Clean(string directory);
}
