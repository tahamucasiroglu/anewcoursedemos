using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Compose;

namespace TahaKonsept.Agent.Abstractions.Compose.Abstract;

/// <summary>
/// Sahne videolarını birleştirir, isteğe bağlı karaoke altyazı (ASS) ve üst ilerleme çubuğu basar, sesi ekler (mux).
/// Seçenekler <see cref="ComposeRequest"/> üzerinden gerçekten uygulanır.
/// </summary>
public interface IComposer
{
    /// <summary>Son videoyu üretir.</summary>
    /// <param name="request">Birleştirme isteği.</param>
    /// <param name="progress">0–100 ilerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Son video.</returns>
    Task<Result<ComposeOutput>> ComposeAsync(ComposeRequest request, IProgress<double>? progress, CancellationToken ct);
}
