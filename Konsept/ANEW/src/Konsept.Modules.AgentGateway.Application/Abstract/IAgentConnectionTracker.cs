using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Açık hub bağlantılarının sahibi ve kesme eylemi (spec 0012 Req 6, 12): kayıt olmamış bağlantı da dahil, iptal edilen
/// cihazın her bağlantısı sunucu tarafından kapatılabilsin diye.
/// </summary>
public interface IAgentConnectionTracker
{
    /// <summary>Bağlantıyı izlemeye alır.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="owner">Sahip ve cihaz.</param>
    /// <param name="abort">Bağlantıyı sunucu tarafında kesen eylem.</param>
    void Track(string connectionId, AgentOwner owner, Action abort);

    /// <summary>Bağlantıyı izlemeden çıkarır.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    void Untrack(string connectionId);

    /// <summary>Verilen cihazların açık bağlantılarını izlemeden çıkarıp döndürür.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="deviceIds">Cihazlar.</param>
    /// <returns>Bağlantı kimlikleri ve kesme eylemleri.</returns>
    IReadOnlyList<KeyValuePair<string, Action>> Take(Guid userId, IReadOnlyCollection<Guid> deviceIds);
}
