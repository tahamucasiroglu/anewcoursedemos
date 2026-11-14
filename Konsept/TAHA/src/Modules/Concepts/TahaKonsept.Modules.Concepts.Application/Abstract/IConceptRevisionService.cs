using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// "Şunları düzelterek tekrar üret" döngüsü (API): talimatla aday üretir (kaydetmez), aday kabul edilince konseptin yerine geçer,
/// atılınca silinir (maliyeti yine konsepte yazılır). "Bunu da düzenle": aday belgesiyle tekrar <see cref="ReviseAsync"/>.
/// </summary>
public interface IConceptRevisionService : IApplicationService
{
    /// <summary>Düzeltme adayı üretir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aday.</returns>
    Task<Result<ConceptRevisionCandidateDto>> ReviseAsync(Guid id, ReviseConceptRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adayı kabul eder (konsept belgesi değişir).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    Task<Result<ConceptDetailDto>> AcceptAsync(Guid id, Guid candidateId, CancellationToken cancellationToken = default);

    /// <summary>Adayı atar.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DiscardAsync(Guid id, Guid candidateId, CancellationToken cancellationToken = default);
}
