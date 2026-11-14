using TahaKonsept.Web.Models;

namespace TahaKonsept.Web.Services.Downloads.Abstract;

/// <summary>
/// Sunucuda yayınlanan masaüstü programı kurulum dosyasının bilgisini verir (<see cref="GetCurrent"/>): dosya var mı,
/// sürümü (Velopack <c>releases.win.json</c>) ve boyutu. Sürüm/boyut asla koda yazılmaz; her çağrıda diskten okunur.
/// </summary>
public interface IAgentReleaseProvider
{
    /// <summary>Güncel kurulum dosyası bilgisini döner (dosya yoksa <see cref="AgentReleaseInfo.IsAvailable"/> = false).</summary>
    AgentReleaseInfo GetCurrent();
}
