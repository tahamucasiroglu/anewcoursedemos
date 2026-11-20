using OkulYonetim.Application.Features.Platform;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Platform paneli için okul özeti (spec 0007): bütün okulları okur (okul filtresini bilerek delen izinli okuma), yalnızca
/// özet alanları döndürür — okulların içindeki veri (kişiler, öğrenciler, şube ayrıntısı) dönmez.
/// </summary>
public interface IPlatformDirectory
{
    Task<SchoolPage> ListAsync(SchoolListFilter filter, CancellationToken cancellationToken);

    Task<SchoolRow?> FindAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<Domain.Organization.Tenant?> FindTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
