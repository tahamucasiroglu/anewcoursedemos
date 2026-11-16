using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konseptler sayfasının üretim alanları (spec 0014 Req 1–21): hazır olma, bekleyen öneriler, iki şeridin işi; Konu üret, Seçtiklerini
/// üret, fikirden Üret ve Durdur. Başlatma istekleri doğrulanır ve hemen döner; üretim arka planda sürer. Reddedilen istekte online
/// çağrı yapılmaz.
/// </summary>
public interface IConceptWorkService
{
    /// <summary>Profilin üretim durumu.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<ConceptWorkDto>> GetAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>Konu üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel durum.</returns>
    Task<Result<ConceptWorkDto>> StartTopicsAsync(StartTopicsRequest? request, CancellationToken cancellationToken);

    /// <summary>Seçtiklerini üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel durum.</returns>
    Task<Result<ConceptWorkDto>> StartFromTopicsAsync(StartFromTopicsRequest? request, CancellationToken cancellationToken);

    /// <summary>Fikirden üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel durum.</returns>
    Task<Result<ConceptWorkDto>> StartFromIdeaAsync(StartFromIdeaRequest? request, CancellationToken cancellationToken);

    /// <summary>Şeritteki işi durdurur.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel durum.</returns>
    Task<Result<ConceptWorkDto>> StopAsync(StopJobRequest? request, CancellationToken cancellationToken);
}
