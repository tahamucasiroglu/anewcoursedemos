using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Hub'a bağlı ajan bağlantılarının bellek içi izleyicisi (singleton). Önemli metotlar: <see cref="Add"/>, <see cref="Remove"/>,
/// <see cref="GetPrimary"/> (kullanıcının komut gönderilecek bağlantısı), <see cref="IsDeviceOnline"/>.
/// </summary>
public interface IAgentConnectionTracker
{
    /// <summary>Bağlantı ekler (aynı kimlik varsa değiştirir).</summary>
    /// <param name="connection">Bağlantı.</param>
    void Add(AgentConnection connection);

    /// <summary>Bağlantıyı çıkarır.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Çıkarılan bağlantı ya da <c>null</c>.</returns>
    AgentConnection? Remove(string connectionId);

    /// <summary>Bağlantıyı getirir.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Bağlantı ya da <c>null</c>.</returns>
    AgentConnection? Get(string connectionId);

    /// <summary>Kullanıcının tüm bağlantıları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bağlantılar.</returns>
    IReadOnlyList<AgentConnection> GetByUser(Guid userId);

    /// <summary>Cihazın tüm bağlantıları.</summary>
    /// <param name="deviceId">Cihaz.</param>
    /// <returns>Bağlantılar.</returns>
    IReadOnlyList<AgentConnection> GetByDevice(Guid deviceId);

    /// <summary>Kullanıcının komut gönderilecek bağlantısı: Hello göndermiş en yeni bağlantı, yoksa en yeni bağlantı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bağlantı ya da <c>null</c>.</returns>
    AgentConnection? GetPrimary(Guid userId);

    /// <summary>Cihazın açık bağlantısı var mı.</summary>
    /// <param name="deviceId">Cihaz.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    bool IsDeviceOnline(Guid deviceId);
}
