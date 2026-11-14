using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konu önerisi servisi (API): aktif profil için ucuz modelle yalnız başlık önerir (hafızadakiler elenir, önerilenler hafızaya yazılır),
/// beğenilmeyenleri negatif örnek olarak kaydeder ve hafızayı sıfırlar.
/// </summary>
public interface ITopicSuggestionService : IApplicationService
{
    /// <summary>Başlık önerir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öneriler.</returns>
    Task<Result<TopicSuggestionsResponse>> SuggestAsync(SuggestTopicsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Beğenilmeyen başlıkları kaydeder.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DislikeAsync(DislikeTopicsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Konu hafızasını sıfırlar.</summary>
    /// <param name="all">true: üretilen/silinen izleri de sil; false: yalnız öneri ve beğenilmeyenler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen kayıt sayısı.</returns>
    Task<Result<TopicMemoryResetResponse>> ResetMemoryAsync(bool all, CancellationToken cancellationToken = default);
}
