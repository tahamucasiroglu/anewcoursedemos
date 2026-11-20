using OkulYonetim.Application.Features.Administration;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Seçili okulun kişileri için okuma (spec 0005): üyelik + kişi + bu okuldaki roller. Kurum filtresiyle yalnızca seçili
/// okul görünür; başka okulda olan kişinin oradaki bilgisi hiçbir satıra girmez.
/// </summary>
public interface IMemberDirectory
{
    Task<MemberPage> ListAsync(MemberListFilter filter, CancellationToken cancellationToken);

    Task<MemberRow?> FindAsync(Guid userId, CancellationToken cancellationToken);
}
