namespace TahaKonsept.Agent.Abstractions.Video;

/// <summary>Render sonucu: video dosyası ve gerçek süresi.</summary>
/// <param name="VideoPath">Üretilen dosya.</param>
/// <param name="DurationSec">Süre (sn).</param>
public sealed record VideoRenderOutput(string VideoPath, double DurationSec);
