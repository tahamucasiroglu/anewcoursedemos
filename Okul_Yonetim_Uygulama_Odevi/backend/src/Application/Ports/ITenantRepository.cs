using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Kurumlar. <c>GetCurrentAsync</c> kurum filtresiyle yalnızca seçili kurumu döndürür. Filtre dışı iki okuma izinli
/// listededir (T5): kişinin üye olduğu kurumların adları (giriş ve okul seçimi) ve kişinin kurucusu olduğu aynı adlı
/// kurum (okul açma komutu, "zaten var" kontrolü).
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Tenant>> ListByIdsAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken cancellationToken);

    Task<Tenant?> FindFoundedByNameAsync(Guid founderUserId, string name, CancellationToken cancellationToken);

    void Add(Tenant tenant);
}
