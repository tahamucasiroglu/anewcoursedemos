using System.Threading.RateLimiting;

namespace Konsept.Modules.Identity.Infrastructure.RateLimiting;

/// <summary><see cref="FixedBlockRateLimiter"/>'ın kirası: izin verildi ya da ne kadar sonra yeniden denenebileceğiyle reddedildi.</summary>
public sealed class FixedBlockLease : RateLimitLease
{
    private readonly TimeSpan? _retryAfter;

    private FixedBlockLease(bool acquired, TimeSpan? retryAfter)
    {
        IsAcquired = acquired;
        _retryAfter = retryAfter;
    }

    /// <summary>Gets izin verilmiş kira.</summary>
    public static FixedBlockLease Granted { get; } = new(true, null);

    /// <inheritdoc />
    public override bool IsAcquired { get; }

    /// <inheritdoc />
    public override IEnumerable<string> MetadataNames =>
        _retryAfter is null ? [] : [MetadataName.RetryAfter.Name];

    /// <summary>Reddedilmiş kira.</summary>
    /// <param name="retryAfter">Yeniden denemeye kalan süre.</param>
    /// <returns>Kira.</returns>
    public static FixedBlockLease Rejected(TimeSpan retryAfter) => new(false, retryAfter);

    /// <inheritdoc />
    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (_retryAfter is { } retry && metadataName == MetadataName.RetryAfter.Name)
        {
            metadata = retry;
            return true;
        }

        metadata = null;
        return false;
    }
}
