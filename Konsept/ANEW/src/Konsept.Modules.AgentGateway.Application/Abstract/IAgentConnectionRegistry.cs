using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Bağlı ajan oturumunun tutulduğu yer. Bu spec'te tek ajan vardır (kimlik 0012'de); en son kaydolan geçerlidir.
/// </summary>
public interface IAgentConnectionRegistry
{
    /// <summary>Gets bağlı ajan oturumu; bağlı ajan yoksa <c>null</c>.</summary>
    AgentSession? Current { get; }

    /// <summary>Oturumu geçerli ajan olarak kaydeder.</summary>
    /// <param name="session">Ajan oturumu.</param>
    void Set(AgentSession session);

    /// <summary>Bağlantı geçerli oturumsa kaldırır.</summary>
    /// <param name="connectionId">Kopan bağlantının kimliği.</param>
    /// <returns>Kaldırılan oturum; bağlantı geçerli oturum değilse <c>null</c>.</returns>
    AgentSession? Remove(string connectionId);
}
