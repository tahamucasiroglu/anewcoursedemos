namespace TahaKonsept.Agent.Abstractions.Speech;

/// <summary>Seslendirme sonucu.</summary>
/// <param name="AudioPath">Üretilen .wav.</param>
/// <param name="DurationSec">Süre (sn).</param>
public sealed record SpeechOutput(string AudioPath, double DurationSec);
