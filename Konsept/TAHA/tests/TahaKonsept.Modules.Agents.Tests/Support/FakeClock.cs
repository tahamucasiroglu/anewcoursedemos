using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Testlerde elle ilerletilen saat (<see cref="Advance"/>).
/// </summary>
public sealed class FakeClock : IClock
{
    /// <summary>Şu an.</summary>
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Saati ilerletir.</summary>
    /// <param name="by">Süre.</param>
    public void Advance(TimeSpan by) => UtcNow += by;
}
