using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konsept üreticisi (LLM, konsept modeli): yapılandırılmış JSON (<c>ConceptLlmOutput</c>) ister ve <see cref="ConceptDocument"/>'e çevirir.
/// Gerçek bilgi açıksa web araması kullanılır. <see cref="ReviseAsync"/> aynı sistem promptuyla (önbellek paylaşılır) düzeltir.
/// </summary>
public interface IConceptGenerator
{
    /// <summary>Konsept üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="brief">Girdi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Üretilen konsept.</returns>
    Task<Result<GeneratedConcept>> GenerateAsync(ConceptGenerationContext context, ConceptBrief brief, CancellationToken cancellationToken = default);

    /// <summary>Konsepti talimata göre düzeltir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="document">Mevcut belge.</param>
    /// <param name="instruction">Talimat.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Düzeltilmiş konsept.</returns>
    Task<Result<GeneratedConcept>> ReviseAsync(ConceptGenerationContext context, ConceptDocument document, string instruction, CancellationToken cancellationToken = default);
}
