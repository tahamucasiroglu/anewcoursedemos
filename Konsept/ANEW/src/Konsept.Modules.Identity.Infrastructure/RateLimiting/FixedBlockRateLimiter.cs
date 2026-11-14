using System.Threading.RateLimiting;

namespace Konsept.Modules.Identity.Infrastructure.RateLimiting;

/// <summary>
/// Kayan pencereli, bloklu hız sınırı (spec 0002 Req 9, review R5-1): verilen izinlerin zamanları tutulur; son
/// <see cref="Window"/> içinde (dahil) zaten <see cref="PermitLimit"/> izin varken gelen istek reddedilir ve
/// <see cref="BlockDuration"/> boyunca her istek reddedilir (blok süresi denemelerle uzamaz); blok bitince sayım sıfırdan
/// başlar. Sabit pencerenin kenarında ~20 deneme geçmesi böylece olmaz. Zaman <see cref="TimeProvider"/>'dan gelir (testte
/// saat ilerletilir). Kuyruk yoktur. Çerçeve adaptörü.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class FixedBlockRateLimiter(TimeProvider timeProvider) : RateLimiter
{
    /// <summary>Pencere başına izin.</summary>
    public const int PermitLimit = 10;

    /// <summary>Kayan pencere süresi.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Aşımdan sonraki blok süresi.</summary>
    public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(1);

    private readonly Lock _gate = new();
    private readonly Queue<DateTimeOffset> _granted = new(PermitLimit);
    private DateTimeOffset _blockedUntil = DateTimeOffset.MinValue;
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
                // Pencerede izin ya da blok varken boşta sayılmaz: bölümlü sınırlayıcı onu atıp sayımı sıfırlamasın.
                var now = timeProvider.GetUtcNow();
                var busyUntil = Max(_granted.Count == 0 ? _lastActivity : _granted.Last() + Window, _blockedUntil);
                return now <= busyUntil ? null : now - busyUntil;
            }
        }
    }

    /// <inheritdoc />
    public override RateLimiterStatistics? GetStatistics()
    {
        lock (_gate)
        {
            Prune(timeProvider.GetUtcNow());
            return new RateLimiterStatistics
            {
                CurrentAvailablePermits = PermitLimit - _granted.Count,
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

            Prune(now);
            if (_granted.Count + permitCount > PermitLimit)
            {
                _blockedUntil = now + BlockDuration;
                _granted.Clear(); // Blok bitince sayım sıfırdan başlar.
                _failed++;
                return FixedBlockLease.Rejected(BlockDuration);
            }

            for (var i = 0; i < permitCount; i++)
            {
                _granted.Enqueue(now);
            }

            _successful++;
            return FixedBlockLease.Granted;
        }
    }

    /// <inheritdoc />
    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) =>
        ValueTask.FromResult(AttemptAcquireCore(permitCount));

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    // Pencere dahildir: tam Window yaşındaki izin hâlâ sayılır (t=0 ve t=60 sn aynı dakikadadır).
    private void Prune(DateTimeOffset now)
    {
        while (_granted.Count > 0 && now - _granted.Peek() > Window)
        {
            _granted.Dequeue();
        }
    }
}
