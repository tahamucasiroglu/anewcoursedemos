namespace TahaKonsept.Agent.Core.VideoServer;

/// <summary>Kapsayıcı bayt aralığı (<c>Start</c>–<c>End</c>, ikisi de dahil).</summary>
/// <param name="Start">İlk bayt.</param>
/// <param name="End">Son bayt (dahil).</param>
public readonly record struct ByteRange(long Start, long End)
{
    /// <summary>Aralıktaki bayt sayısı.</summary>
    public long Length => End - Start + 1;
}
