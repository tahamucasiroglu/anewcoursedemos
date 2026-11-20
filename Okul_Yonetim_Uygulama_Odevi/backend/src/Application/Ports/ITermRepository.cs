using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Dönemler (spec 0008) — her okuma kurum filtresinden geçer: yalnızca seçili okulun dönemleri görünür (S1). Okul başına
/// dönem sayısı küçüktür; çakışma kontrolü listede yapılır.
/// </summary>
public interface ITermRepository
{
    Task<IReadOnlyList<Term>> ListAsync(CancellationToken cancellationToken);

    Task<Term?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Seçili okulda aynı adlı (Türkçe harf kuralıyla, harf farkı gözetmeden) dönem; <paramref name="exceptId"/> hariç.</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Term term);
}
