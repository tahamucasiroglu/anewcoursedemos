using Konsept.Shared.Logging.Abstract;

namespace Konsept.Shared.Logging;

/// <summary>
/// Log klasörü temizliğini açılışta ve sonra günde bir çalıştırır (spec 0008 Req 4). Barındırmadan bağımsızdır;
/// Web, Api ve ajan aynı şekilde kullanır. Temizliğin kendisi <see cref="ILogFileJanitor"/>'dadır.
/// </summary>
/// <param name="janitor">Temizleyici.</param>
/// <param name="directory">Log klasörü.</param>
/// <param name="timeProvider">Zaman kaynağı.</param>
public sealed class LogFileCleanupScheduler(ILogFileJanitor janitor, string directory, TimeProvider timeProvider) : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private ITimer? _timer;

    /// <summary>Hemen bir temizlik yapar ve günlük zamanlayıcıyı kurar.</summary>
    /// <returns>Aynı nesne.</returns>
    public LogFileCleanupScheduler Start()
    {
        _timer ??= timeProvider.CreateTimer(_ => Run(), null, TimeSpan.Zero, Interval);
        return this;
    }

    /// <inheritdoc />
    public void Dispose() => _timer?.Dispose();

    private void Run()
    {
        try
        {
            janitor.Clean(directory);
        }
        catch (IOException)
        {
            // Klasöre o an erişilemiyor — ertesi gün yeniden denenir; log yazımı etkilenmez.
        }
        catch (UnauthorizedAccessException)
        {
            // Klasör izni bozuk — zamanlayıcı geri çağrısında yakalanmayan istisna süreci düşürürdü (review L-3).
        }
    }
}
