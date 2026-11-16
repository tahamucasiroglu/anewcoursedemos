using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Bağlı ajan oturumlarının tutulduğu yer (spec 0012 Req 6, 13): kullanıcı başına tek ajan; aynı kullanıcının en son kaydolan
/// ajanı geçerlidir. Bir kullanıcı yalnızca kendi ajanını görür.
/// </summary>
public interface IAgentConnectionRegistry
{
    /// <summary>Kullanıcının bağlı ajanını bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Oturum ya da <c>null</c>.</returns>
    AgentSession? Find(Guid userId);

    /// <summary>Bağlantı kimliğiyle geçerli oturumu bulur.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <returns>Oturum ya da <c>null</c>.</returns>
    AgentSession? FindByConnection(string connectionId);

    /// <summary>Oturumu sahibinin geçerli ajanı olarak kaydeder.</summary>
    /// <param name="session">Ajan oturumu.</param>
    void Set(AgentSession session);

    /// <summary>Bağlantı geçerli oturumsa kaldırır.</summary>
    /// <param name="connectionId">Kopan bağlantının kimliği.</param>
    /// <returns>Kaldırılan oturum; bağlantı geçerli oturum değilse <c>null</c>.</returns>
    AgentSession? Remove(string connectionId);
}
