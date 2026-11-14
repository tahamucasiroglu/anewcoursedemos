using System.Diagnostics;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Bir koşulun bir süre sınırı içinde sağlanmasını bekler (ör. "en geç 10 sn içinde ajan bağlı").
/// </summary>
public static class Eventually
{
    /// <summary>Koşul sağlanana ya da süre dolana kadar kısa aralıklarla yoklar.</summary>
    /// <param name="condition">Koşul.</param>
    /// <param name="limit">En uzun bekleme.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Koşulun sağlandığı süre.</returns>
    /// <exception cref="TimeoutException">Süre dolarsa.</exception>
    public static async Task<TimeSpan> WithinAsync(Func<Task<bool>> condition, TimeSpan limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < limit)
        {
            if (await condition().ConfigureAwait(false))
            {
                return watch.Elapsed;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Koşul {limit.TotalSeconds} sn içinde sağlanmadı.");
    }
}
