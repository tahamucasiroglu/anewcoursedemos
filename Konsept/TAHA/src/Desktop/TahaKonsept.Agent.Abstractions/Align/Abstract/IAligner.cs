using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Align;

namespace TahaKonsept.Agent.Abstractions.Align.Abstract;

/// <summary>
/// Seslendirme dosyası ile metni hizalayıp kelime bazlı zamanlama üretir (karaoke altyazı için).
/// Okunuş (ses) ile yazılış (ekran) farklı olabileceğinden çıktı <b>yazılış</b> kelimeleriyle döner.
/// </summary>
public interface IAligner
{
    /// <summary>Hizalayıcı anahtarı (ör. <c>proportional</c>, <c>whisper</c>).</summary>
    string Key { get; }

    /// <summary>Hizalamayı yapar.</summary>
    /// <param name="request">Ses ve segmentler.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kelime zamanlamaları.</returns>
    Task<Result<IReadOnlyList<CaptionWord>>> AlignAsync(AlignRequest request, CancellationToken ct);
}
