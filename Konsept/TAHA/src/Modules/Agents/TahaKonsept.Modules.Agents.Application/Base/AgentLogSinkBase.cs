using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Ajan günlük alıcısı tabanı: kaynak metnini (<see cref="FormatSource"/>: <c>agent/{kategori}</c>) üretir; yazma türetilen sınıftadır.
/// </summary>
public abstract class AgentLogSinkBase : IAgentLogSink
{
    /// <summary>Günlük kaydını yazar.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public abstract Task WriteAsync(AgentCallContext agent, AgentLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Kaydın kaynak metni.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <returns>Kaynak (ör. <c>agent/Speech</c>).</returns>
    protected static string FormatSource(AgentLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return string.IsNullOrWhiteSpace(entry.Category) ? "agent" : "agent/" + entry.Category.Trim();
    }
}
