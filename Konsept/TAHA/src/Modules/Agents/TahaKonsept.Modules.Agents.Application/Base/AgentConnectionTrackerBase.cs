using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Bağlantı izleyici tabanı: saklama (<see cref="Add"/>, <see cref="Remove"/>, <see cref="Get"/>, <see cref="Snapshot"/>) türetilen sınıfta;
/// sorgular (kullanıcıya/cihaza göre, birincil bağlantı seçimi, çevrimiçi kontrolü) burada.
/// </summary>
public abstract class AgentConnectionTrackerBase : IAgentConnectionTracker
{
    /// <summary>Bağlantı ekler.</summary>
    /// <param name="connection">Bağlantı.</param>
    public abstract void Add(AgentConnection connection);

    /// <summary>Bağlantıyı çıkarır.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Çıkarılan bağlantı.</returns>
    public abstract AgentConnection? Remove(string connectionId);

    /// <summary>Bağlantıyı getirir.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Bağlantı.</returns>
    public abstract AgentConnection? Get(string connectionId);

    /// <summary>Kullanıcının bağlantıları (en yeni önce).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bağlantılar.</returns>
    public IReadOnlyList<AgentConnection> GetByUser(Guid userId) =>
        Snapshot().Where(c => c.UserId == userId).OrderByDescending(c => c.ConnectedAtUtc).ToList();

    /// <summary>Cihazın bağlantıları.</summary>
    /// <param name="deviceId">Cihaz.</param>
    /// <returns>Bağlantılar.</returns>
    public IReadOnlyList<AgentConnection> GetByDevice(Guid deviceId) =>
        Snapshot().Where(c => c.DeviceId == deviceId).ToList();

    /// <summary>Birincil bağlantı: Hello göndermiş en yeni, yoksa en yeni bağlantı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bağlantı ya da <c>null</c>.</returns>
    public AgentConnection? GetPrimary(Guid userId)
    {
        var connections = GetByUser(userId);
        return connections.FirstOrDefault(c => c.HelloReceived) ?? connections.FirstOrDefault();
    }

    /// <summary>Cihaz çevrimiçi mi.</summary>
    /// <param name="deviceId">Cihaz.</param>
    /// <returns>Bağlantısı varsa <c>true</c>.</returns>
    public bool IsDeviceOnline(Guid deviceId) => Snapshot().Any(c => c.DeviceId == deviceId);

    /// <summary>Tüm bağlantıların anlık kopyası.</summary>
    /// <returns>Bağlantılar.</returns>
    protected abstract IReadOnlyCollection<AgentConnection> Snapshot();
}
