using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Kelime sayısına göre süre tahmini yapan sahne planlayıcı: Türkçe anlatımda saniyede ~<see cref="WordsPerSecond"/> kelime,
/// üstüne <see cref="PaddingSec"/> sn nefes payı (sahne render'ı biraz uzun olsun; birleştirmede gerçek ses süresine kırpılır/uzatılır).
/// </summary>
public sealed class NarrationScenePlanner : ScenePlannerBase
{
    /// <summary>Saniyedeki kelime.</summary>
    public const double WordsPerSecond = 2.2;

    /// <summary>Sahne sonu payı (sn).</summary>
    public const double PaddingSec = 0.8;

    /// <summary>Süre tahmini.</summary>
    /// <param name="narration">Seslendirme.</param>
    /// <returns>Süre (sn).</returns>
    protected override double EstimateDurationSec(string narration)
    {
        var words = narration.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return words == 0 ? 0 : (words / WordsPerSecond) + PaddingSec;
    }
}
