using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Tests.Support;

/// <summary>
/// Test saati: gerçek zamandan başlar, <see cref="Advance"/> ile ileri alınır.
/// </summary>
public sealed class FakeClock : IClock
{
    /// <summary>Şu anki zaman.</summary>
    public DateTimeOffset UtcNow { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Saati ileri alır.</summary>
    /// <param name="by">Süre.</param>
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
