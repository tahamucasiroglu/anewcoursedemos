namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Ajanın indirip kurabildiği bir bileşen (spec 0027): kimlik, görünen ad, yaklaşık indirme ve kurulu boyut (bayt).</summary>
/// <param name="Id">Bileşen kimliği (ör. <c>hyperframes</c>).</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="DownloadBytes">Yaklaşık indirme boyutu.</param>
/// <param name="InstalledBytes">Yaklaşık diskte kaplayacağı alan.</param>
public sealed record AgentComponentInfo(string Id, string Name, long DownloadBytes, long InstalledBytes);
