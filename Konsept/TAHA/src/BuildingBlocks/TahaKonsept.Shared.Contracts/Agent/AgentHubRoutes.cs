namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajan SignalR hub'ının yolu ve kimlik doğrulama sorgu parametresi.
/// </summary>
public static class AgentHubRoutes
{
    /// <summary>Hub yolu.</summary>
    public const string HubPath = "/hubs/agent";

    /// <summary>JWT'nin taşındığı sorgu parametresi (SignalR standardı).</summary>
    public const string AccessTokenQueryKey = "access_token";
}
