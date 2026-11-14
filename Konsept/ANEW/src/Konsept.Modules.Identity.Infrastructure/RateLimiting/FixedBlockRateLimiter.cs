using System.Threading.RateLimiting;

namespace Konsept.Modules.Identity.Infrastructure.RateLimiting;

/// <summary>
/// Sabit pencereli, bloklu hız sınırı (spec 0002 Req 9): pencere içinde <see cref="PermitLimit"/> izin; aşan ilk istekte
/// <see cref="BlockDuration"/> boyunca her istek reddedilir (blok süresi denemelerle uzamaz), sonra yeni pencere başlar.
/// Zaman <see cref="TimeProvider"/>'dan gelir (testte saat ilerletilir). Kuyruk yoktur. Çerçeve adaptörü.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class FixedBlockRateLimiter(TimeProvider timeProvider) : RateLimiter
{
    /// <summary>Pencere başına izin.</summary>
    public const int PermitLimit = 10;

    /// <summary>Pencere süresi.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Aşımdan sonraki blok süresi.</summary>
    public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(1);

    private readonly Lock _gate = new();
    private DateTimeOffset _windowStart = DateTimeOffset.MinValue;
    private DateTimeOffset _blockedUntil = DateTimeOffset.MinValue;
    private int _used;
    private long _successful;
    private long _failed;
    private DateTimeOffset _lastActivity = timeProvider.GetUtcNow();

    /// <inheritdoc />
    public override TimeSpan? IdleDuration
    {
        get
        {
            lock (_gate)
            {
                // Pencere ya da blok sürerken boşta sayılmaz: bölümlü sınırlayıcı onu atıp sayacı sıfırlamasın.
                var now = timeProvider.GetUtcNow();
                var busyUntil = _windowStart == DateTimeOffset.MinValue ? _lastActivity : Max(_windowStart + Window, _blockedUntil);
                return now < busyUntil ? null : now - busyUntil;
            }
        }
    }

    /// <inheritdoc />
    public override RateLimiterStatistics? GetStatistics()
    {
        lock (_gate)
        {
            return new RateLimiterStatistics
            {
                CurrentAvailablePermits = Math.Max(0, PermitLimit - _used),
                CurrentQueuedCount = 0,
                TotalSuccessfulLeases = _successful,
                TotalFailedLeases = _failed,
            };
        }
    }

    /// <inheritdoc />
    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        lock (_gate)
        {
            var now = timeProvider.GetUtcNow();
            _lastActivity = now;
            if (now < _blockedUntil)
            {
                _failed++;
                return FixedBlockLease.Rejected(_blockedUntil - now);
            }

            if (now - _windowStart >= Window)
            {
                _windowStart = now;
                _used = 0;
            }

            if (_used + permitCount > PermitLimit)
            {
                _blockedUntil = now + BlockDuration;
                _failed++;
                return FixedBlockLease.Rejected(BlockDuration);
            }

            _used += permitCount;
            _successful++;
            return FixedBlockLease.Granted;
        }
    }

    /// <inheritdoc />
    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) =>
        ValueTask.FromResult(AttemptAcquireCore(permitCount));

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
