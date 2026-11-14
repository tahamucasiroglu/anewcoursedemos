namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// "Ajanı test et" cevabı.
/// </summary>
/// <param name="MachineName">Ajanın bilgisayar adı.</param>
/// <param name="AgentVersion">Ajanın versiyonu.</param>
public sealed record AgentTestResponse(string MachineName, string AgentVersion);
