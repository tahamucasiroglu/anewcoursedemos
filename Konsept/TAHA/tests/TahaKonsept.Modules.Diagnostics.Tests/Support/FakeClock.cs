using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Diagnostics.Tests.Support;

/// <summary>
/// Elle ilerletilen test saati (<see cref="Advance"/>).
/// </summary>
public sealed class FakeClock : IClock
{
    /// <summary>Oluşturur.</summary>
    /// <param name="start">Başlangıç anı (UTC).</param>
    public FakeClock(DateTimeOffset start)
    {
        UtcNow = start;
    }

    /// <summary>Şu an.</summary>
    public DateTimeOffset UtcNow { get; private set; }

    /// <summary>Saati ileri alır.</summary>
    /// <param name="by">Süre.</param>
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
