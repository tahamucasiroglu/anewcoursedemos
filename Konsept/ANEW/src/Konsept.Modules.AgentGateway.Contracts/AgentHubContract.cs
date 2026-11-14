namespace Konsept.Modules.AgentGateway.Contracts;

/// <summary>
/// API ile ajan arasındaki SignalR sözleşmesi: hub yolu ve metot adları. İki taraf da bu sabitleri kullanır.
/// </summary>
public static class AgentHubContract
{
    /// <summary>Hub'ın API üzerindeki yolu.</summary>
    public const string HubPath = "/hubs/agent";

    /// <summary>Ajan → API: bağlanınca (ve her yeniden bağlanmada) kendini kaydeder.</summary>
    public const string Register = "Register";

    /// <summary>API → ajan: "Ajanı test et" komutu; ajan sürümünü ve makine adını döndürür.</summary>
    public const string Ping = "Ping";
}
