using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konsept düzenleme döngüsü (spec 0015): doğrudan düzenleme, talimatla yeniden üretim, revizyonu onaylama/silme. Yalnızca kendi
/// konseptleri (BR-1) ve yalnızca Üretildi/Onaylandı durumunda (S-5).
/// </summary>
public interface IConceptEditService
{
    /// <summary>Sahneleri kaydeder (çağrı yok).</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="request">Sahneler.</param>
    /// <param name="regen">Yeniden üret bağlamı belirteci (spec 0020 Req 15): yalnızca geçerliyse "Video oldu" konsept düzenlenir.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    Task<Result<ConceptDetailDto>> SaveScenesAsync(Guid conceptId, SaveScenesRequest? request, string? regen, CancellationToken cancellationToken);

    /// <summary>Talimatla yeniden üretimi arka planda başlatır ve hemen döner.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="request">Talimat ve kaydedilmemiş sahneler.</param>
    /// <param name="regen">Yeniden üret bağlamı belirteci (spec 0020 Req 15): yalnızca geçerliyse "Video oldu" konsept düzenlenir.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu.</returns>
    Task<Result<RevisionDto>> RegenerateAsync(Guid conceptId, RegenerateRequest? request, string? regen, CancellationToken cancellationToken);

    /// <summary>Revizyon durumu.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu.</returns>
    Task<Result<RevisionDto>> GetRevisionAsync(Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Hazır revizyonu konseptin yerine koyar (durum değişmez).</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="regen">Yeniden üret bağlamı belirteci (spec 0020 Req 15): yalnızca geçerliyse "Video oldu" konsept düzenlenir.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    Task<Result<ConceptDetailDto>> ApproveRevisionAsync(Guid conceptId, string? regen, CancellationToken cancellationToken);

    /// <summary>Revizyonu atar; konsept aynen kalır.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu (yok).</returns>
    Task<Result<RevisionDto>> DiscardRevisionAsync(Guid conceptId, CancellationToken cancellationToken);
}
