using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Her okumada 1 sn ilerleyen sahte saat; <see cref="Advance"/> ile ileri sarılır.
/// </summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _now = new(2026, 11, 14, 7, 30, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public DateTimeOffset UtcNow
    {
        get
        {
            lock (this)
            {
                _now = _now.AddSeconds(1);
                return _now;
            }
        }
    }

    /// <summary>Saati ileri sarar.</summary>
    /// <param name="by">Süre.</param>
    public void Advance(TimeSpan by)
    {
        lock (this)
        {
            _now = _now.Add(by);
        }
    }
}
