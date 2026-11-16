namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın bağlanınca (ve bir bileşen kurulunca yeniden) gönderdiği kimlik bilgisi.
/// </summary>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan (ürün) versiyonu.</param>
/// <param name="InstalledComponents">Kurulu isteğe bağlı bileşenlerin kimlikleri (spec 0006 Req 19; ör. <c>remotion</c>); yoksa <c>null</c>.</param>
public sealed record AgentRegistration(string MachineName, string AgentVersion, IReadOnlyList<string>? InstalledComponents = null);
