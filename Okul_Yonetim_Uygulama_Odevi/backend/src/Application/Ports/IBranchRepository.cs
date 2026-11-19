using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Şubeler — her okuma kurum + şube filtresinden geçer: yalnızca seçili kurumun, kurucu değilse yalnızca atanılan
/// şubeler görünür (S1). Görünmeyen şube "yok" sayılır.
/// </summary>
public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> ListVisibleAsync(CancellationToken cancellationToken);

    Task<Branch?> FindVisibleAsync(Guid id, CancellationToken cancellationToken);

    void Add(Branch branch);
}
