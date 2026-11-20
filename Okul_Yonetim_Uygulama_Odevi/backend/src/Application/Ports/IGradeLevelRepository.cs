using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Sınıf seviyeleri (spec 0008) — her okuma kurum filtresinden geçer: yalnızca seçili okulun seviyeleri görünür (S1);
/// başka okulun seviyesi "yok" sayılır.
/// </summary>
public interface IGradeLevelRepository
{
    Task<IReadOnlyList<GradeLevel>> ListAsync(CancellationToken cancellationToken);

    Task<GradeLevel?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Seçili okulda aynı adlı (Türkçe harf kuralıyla, harf farkı gözetmeden) etkin seviye; <paramref name="exceptId"/> hariç.</summary>
    Task<bool> ActiveNameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(GradeLevel gradeLevel);
}
