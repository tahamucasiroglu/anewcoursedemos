using Konsept.Modules.AgentGateway.Contracts.Abstract;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>Ajanın bildirdiği kurulu bileşenleri kullanıcı başına saklar (spec 0006 Req 19); bağlantı kopsa da kalır.</summary>
public interface IInstalledComponentStore : IInstalledComponentsReader
{
    /// <summary>Kullanıcının listesini değiştirir.</summary>
    /// <param name="userId">Kullanıcı (hub kimliğinden).</param>
    /// <param name="components">Bileşenler.</param>
    void Set(Guid userId, IEnumerable<string> components);
}
