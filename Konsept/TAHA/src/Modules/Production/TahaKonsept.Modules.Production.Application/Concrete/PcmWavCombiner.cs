using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// PCM (16/24/32 bit) ve IEEE float WAV'ları birleştiren varsayılan birleştirici (singleton).
/// </summary>
public sealed class PcmWavCombiner : WavCombinerBase
{
    /// <summary>PCM ya da float, 1-2 kanal, geçerli örnekleme hızı.</summary>
    /// <param name="format">Biçim.</param>
    /// <returns>Kabul ediliyorsa <c>true</c>.</returns>
    protected override bool IsSupported(WavFormat format) =>
        format.FormatTag is 1 or 3 or 0xFFFE
        && format.Channels is 1 or 2
        && format.SampleRate is >= 8000 and <= 192000
        && format.BlockAlign > 0;
}
