namespace TahaKonsept.Agent.Core.VideoServer;

/// <summary><c>Range</c> başlığı çözüm sonucu.</summary>
/// <param name="Outcome">Sonuç türü.</param>
/// <param name="Range">Aralık (<see cref="RangeParseOutcome.Partial"/> ise).</param>
public sealed record RangeParseResult(RangeParseOutcome Outcome, ByteRange? Range = null);
