using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Toplu konsept üretimini başlatan servis (API): bütçe kontrolünden sonra işi kuyruğa alır ve hemen döner;
/// ilerleme <see cref="GetCurrent"/> ile sorgulanır (Web polling). Kullanıcı başına işler sırayla çalışır.
/// </summary>
public interface IConceptGenerationService : IApplicationService
{
    /// <summary>Seçilen başlıklardan üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş durumu.</returns>
    Task<Result<ConceptJobDto>> StartFromTopicsAsync(GenerateFromTopicsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fikirden üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş durumu.</returns>
    Task<Result<ConceptJobDto>> StartFromIdeaAsync(GenerateFromIdeaRequest request, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının güncel (çalışan, sıradaki ya da en son biten) işi.</summary>
    /// <returns>İş durumu (yoksa <c>Idle</c>).</returns>
    Result<ConceptJobDto> GetCurrent();

    /// <summary>Kullanıcının çalışan ve sıradaki işlerini iptal eder.</summary>
    /// <returns>Güncel iş durumu.</returns>
    Result<ConceptJobDto> CancelCurrent();
}
