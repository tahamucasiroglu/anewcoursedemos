using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Online TTS'ten gelen sahne WAV'larını (aynı PCM biçiminde) tek WAV'da birleştirir; boş parçalar için sessizlik üretir, süreleri hesaplar.
/// </summary>
public interface IWavCombiner
{
    /// <summary>Birleştirir.</summary>
    /// <param name="parts">Sahne sesleri (boş sahne için <c>null</c>).</param>
    /// <param name="silenceSec">Boş sahne sessizlik süresi.</param>
    /// <returns>Sonuç.</returns>
    Result<CombinedAudio> Combine(IReadOnlyList<byte[]?> parts, double silenceSec);
}
