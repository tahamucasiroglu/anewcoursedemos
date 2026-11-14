using TahaKonsept.Modules.Agents.Application.Base;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Varsayılan aşama olay alıcısı: hiçbir şey yapmaz. Production modülü gerçek alıcısını kaydedene kadar kullanılır (TryAdd).
/// </summary>
public sealed class NullAgentStageEventHandler : AgentStageEventHandlerBase
{
}
