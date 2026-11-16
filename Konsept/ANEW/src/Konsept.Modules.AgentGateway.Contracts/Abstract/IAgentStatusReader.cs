using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Contracts.Abstract;

/// <summary>
/// Ajanın anlık durumunu diğer modüllere açar (ör. sistem durumu). Kullanıcı yalnızca kendi ajanını görür (spec 0012 Req 13).
/// </summary>
public interface IAgentStatusReader
{
    /// <summary>Kullanıcının ajanının anlık durumunu döndürür.</summary>
    /// <param name="userId">Kullanıcı; giriş yapılmamışsa <c>null</c> (her zaman "bağlı değil").</param>
    /// <returns>Ajan durumu.</returns>
    AgentStatusDto GetStatus(Guid? userId);
}
