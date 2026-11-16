using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>Ajanın yerel logundaki son Warning+ satırlar (spec 0022 Req 4: rapora son 15 dakikanın satırları eklenir).</summary>
public interface IRecentLogSource
{
    /// <summary>Tutulan satırların anlık kopyası (eskiden yeniye).</summary>
    /// <returns>Satırlar.</returns>
    IReadOnlyList<AgentLogLine> Snapshot();
}
