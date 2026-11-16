using Konsept.Modules.Monitoring.Contracts.Abstract;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek video sürüm deposundan klip istatistiği: konsept başına ilk tamamlanma (gerçek sorgu entegrasyon testinde).</summary>
/// <param name="videos">Bellek video sürüm deposu.</param>
public sealed class InMemoryClipStatistics(InMemoryProducedVideoRepository videos) : IClipStatistics
{
    /// <inheritdoc />
    public Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DateTimeOffset>>([.. videos.Rows.GroupBy(v => v.ConceptId).Select(g => g.Min(v => v.EndedAt))]);
}
