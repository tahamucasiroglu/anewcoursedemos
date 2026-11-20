using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Üyelikler (spec 0005). Okumalar kurum filtresinden geçer: yalnızca seçili okulun üyelikleri görünür.</summary>
public interface ITenantMembershipRepository
{
    Task<TenantMembership?> FindInCurrentTenantAsync(Guid userId, CancellationToken cancellationToken);

    void Add(TenantMembership membership);
}
