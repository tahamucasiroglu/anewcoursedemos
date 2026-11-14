namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Zaman kaynağı (test edilebilirlik için). Tüm zamanlar UTC'dir.
/// </summary>
public interface IClock
{
    /// <summary>Şu anki zaman (UTC, offset 0).</summary>
    DateTimeOffset UtcNow { get; }
}
