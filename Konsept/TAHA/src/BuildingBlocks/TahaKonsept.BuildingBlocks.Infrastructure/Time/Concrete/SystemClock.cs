using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Time.Concrete;

/// <summary>
/// Gerçek sistem saati (<see cref="IClock"/>); <see cref="UtcNow"/> = <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>Şu anki zaman (UTC).</summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
