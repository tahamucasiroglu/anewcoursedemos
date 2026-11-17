using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Üretimin istediği eksik bileşenler (spec 0027): ajanın gömülü listesinden hangilerinin kurulabildiğini söyler ve kullanıcı onayladıktan
/// sonra sunucunun komutuyla kurulum motoru (0009) üzerinden kurar. Yalnızca listedeki kimlikler; adres ve SHA-256 listeden gelir.
/// </summary>
public interface IComponentInstaller
{
    /// <summary>Verilen kimliklerden listede olanların bilgisi (ad, yaklaşık boyutlar).</summary>
    /// <param name="componentIds">Bileşen kimlikleri.</param>
    /// <returns>Kurulabilir bileşenler.</returns>
    IReadOnlyList<AgentComponentInfo> Describe(IEnumerable<string> componentIds);

    /// <summary>
    /// Bileşenleri kurar; ilk kurulum sürüyorsa bitmesini bekler. Listede olmayan kimlik varsa hiçbir şey kurmaz. Hata fırlatmaz (iptal hariç).
    /// </summary>
    /// <param name="request">Kurulacak bileşenler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç: hepsi kurulduysa <c>Ok</c>, kurulamayanlar.</returns>
    Task<AgentInstallReply> InstallAsync(AgentInstallRequest request, CancellationToken cancellationToken);
}
