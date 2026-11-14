namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Birleştirilmiş WAV sonucu (<c>IWavCombiner</c>): içerik, parça süreleri ve toplam süre.
/// </summary>
/// <param name="Audio">WAV içeriği.</param>
/// <param name="DurationsSec">Parça süreleri (sn, sıralı).</param>
/// <param name="TotalSec">Toplam süre (sn).</param>
public sealed record CombinedAudio(byte[] Audio, IReadOnlyList<double> DurationsSec, double TotalSec);
