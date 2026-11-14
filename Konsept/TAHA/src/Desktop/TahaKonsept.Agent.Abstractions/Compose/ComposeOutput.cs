namespace TahaKonsept.Agent.Abstractions.Compose;

/// <summary>Birleştirme sonucu.</summary>
/// <param name="VideoPath">Son video.</param>
/// <param name="DurationSec">Süre (sn).</param>
/// <param name="ThumbnailPath">Küçük resim (jpg).</param>
public sealed record ComposeOutput(string VideoPath, double DurationSec, string? ThumbnailPath);
