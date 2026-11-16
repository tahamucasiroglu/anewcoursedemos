namespace Konsept.Modules.AgentGateway.Contracts.Abstract;

/// <summary>
/// Kullanıcının ajanının son bildirdiği kurulu isteğe bağlı bileşenler (spec 0006 Req 19). Ajan hiç bağlanmadıysa boş; başka kullanıcının
/// ajanının bildirimi bu listeyi etkilemez (BR-1).
/// </summary>
public interface IInstalledComponentsReader
{
    /// <summary>Kurulu bileşen kimlikleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Kimlikler (ordinal).</returns>
    IReadOnlySet<string> Get(Guid userId);
}
