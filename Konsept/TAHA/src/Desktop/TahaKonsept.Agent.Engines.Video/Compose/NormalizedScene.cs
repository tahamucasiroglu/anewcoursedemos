namespace TahaKonsept.Agent.Engines.Video.Compose;

/// <summary>Normalize edilmiş sahne: hedef boyut/fps'teki ara dosya ve süresi.</summary>
/// <param name="Path">Ara dosya.</param>
/// <param name="DurationSec">Süre (sn, hedef süre).</param>
public sealed record NormalizedScene(string Path, double DurationSec);
