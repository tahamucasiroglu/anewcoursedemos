using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Açık hub bağlantılarının sahibi ve kesme eylemi (spec 0012 Req 6, 12): kayıt olmamış bağlantı da dahil, iptal edilen
/// cihazın her bağlantısı sunucu tarafından kapatılabilsin diye.
/// </summary>
public interface IAgentConnectionTracker
{
    /// <summary>
    /// Bağlantıyı izlemeye alır. Cihaz bu süreçte iptal edilmişse (kimlik doğrulaması ile bağlanma arasındaki yarış, review r1
    /// M-1) bağlantı alınmaz.
    /// </summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="owner">Sahip ve cihaz.</param>
    /// <param name="abort">Bağlantıyı sunucu tarafında kesen eylem.</param>
    /// <returns>Alındıysa <c>true</c>; cihaz iptal edilmişse <c>false</c> (çağıran bağlantıyı keser).</returns>
    bool Track(string connectionId, AgentOwner owner, Action abort);

    /// <summary>Bağlantı izleniyor mu (iptal edilmemiş cihazın açık bağlantısı).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <returns>İzleniyorsa <c>true</c>.</returns>
    bool Contains(string connectionId);

    /// <summary>Bağlantıyı izlemeden çıkarır.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    void Untrack(string connectionId);

    /// <summary>Verilen cihazları iptal edilmiş olarak işaretler; açık bağlantılarını izlemeden çıkarıp döndürür.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="deviceIds">Cihazlar.</param>
    /// <returns>Bağlantı kimlikleri ve kesme eylemleri.</returns>
    IReadOnlyList<KeyValuePair<string, Action>> Take(Guid userId, IReadOnlyCollection<Guid> deviceIds);
}
