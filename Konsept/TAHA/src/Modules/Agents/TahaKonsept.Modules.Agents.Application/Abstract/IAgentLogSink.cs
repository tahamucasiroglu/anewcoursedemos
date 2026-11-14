using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ajan günlük kayıtlarının alıcısı (uzatma noktası). Varsayılan kayıt Serilog'a yazar (<c>LoggingAgentLogSink</c>, TryAdd);
/// Diagnostics modülü kullanıcıya görünen günlüğe (<c>diagnostics.activity_logs</c>) yazan adaptörünü kaydeder. Scoped çözülür.
/// </summary>
public interface IAgentLogSink
{
    /// <summary>Günlük kaydını yazar.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task WriteAsync(AgentCallContext agent, AgentLogEntry entry, CancellationToken cancellationToken = default);
}
