using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Sınıflar (spec 0008) — her okuma kurum + şube filtresinden geçer: kurucu okulun bütün sınıflarını, diğer roller
/// yalnızca atandıkları etkin şubelerin sınıflarını görür (S1). Görünmeyen sınıf "yok" sayılır.
/// </summary>
public interface IClassroomRepository
{
    Task<IReadOnlyList<Classroom>> ListByTermAsync(Guid termId, CancellationToken cancellationToken);

    Task<Classroom?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Verilen kimliklerden görünür olan sınıflar (spec 0017: sınıf kapsamlı duyurunun şubesi); izlemesiz.</summary>
    Task<IReadOnlyList<Classroom>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Aynı dönem ve şubede aynı adlı (Türkçe harf kuralıyla, harf farkı gözetmeden) etkin sınıf; <paramref name="exceptId"/> hariç.</summary>
    Task<bool> ActiveNameExistsAsync(Guid termId, Guid branchId, string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Classroom classroom);
}
