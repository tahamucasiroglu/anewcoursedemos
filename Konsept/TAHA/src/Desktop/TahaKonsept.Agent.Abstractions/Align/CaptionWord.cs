namespace TahaKonsept.Agent.Abstractions.Align;

/// <summary>Altyazı kelimesi ve zamanı (ms).</summary>
/// <param name="Text">Kelime (yazılış).</param>
/// <param name="StartMs">Başlangıç.</param>
/// <param name="EndMs">Bitiş.</param>
public sealed record CaptionWord(string Text, int StartMs, int EndMs);
