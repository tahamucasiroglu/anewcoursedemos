using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Production) konsept okuması için PUBLIC sözleşme. Oturum gerektirmez (arka plan işlerinde çalışır);
/// kullanıcı filtresi açıkça verilir. Silinmiş konseptler de döner (durum alanına bakın).
/// </summary>
public interface IConceptReader
{
    /// <summary>Konsepti getirir.</summary>
    /// <param name="userId">Sahip kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ayrıntı ya da null.</returns>
    Task<ConceptDetailDto?> GetAsync(Guid userId, Guid conceptId, CancellationToken ct = default);
}
