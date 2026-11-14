using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Kelime tekrar denetimi: önce yerel analiz (<see cref="IWordRepetitionAnalyzer"/>), bulgu varsa yalnız ilgili sahnelerin
/// seslendirmesini ucuz modele yeniden yazdırır (token tasarrufu). Uzunluğu bozulan yanıtlar uygulanmaz.
/// </summary>
public interface IWordRepetitionChecker
{
    /// <summary>Belgeyi denetler.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="document">Belge.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<WordCheckOutcome>> CheckAsync(ConceptGenerationContext context, ConceptDocument document, CancellationToken cancellationToken = default);
}
