using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Sabit saat (varsayılan 2026-11-14 12:00 UTC).
/// </summary>
public sealed class FixedClock : IClock
{
    /// <summary>Kurucu.</summary>
    /// <param name="now">An (null = 2026-11-14 12:00 UTC).</param>
    public FixedClock(DateTimeOffset? now = null)
    {
        UtcNow = now ?? new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; set; }
}
