namespace TahaKonsept.Modules.Agents.Application.Models;

/// <summary>
/// Eşleme tamamlanınca ajan için token üretme isteği (<c>IAgentTokenIssuer.IssueAsync</c>).
/// Token, kullanıcının kimliğini (<c>sub</c>) ve <c>agent_device_id</c> claim'ini taşımalıdır; refresh token ailesi web'den ayrıdır.
/// </summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="AgentDeviceId">Eşlenmiş cihaz.</param>
/// <param name="MachineName">Bilgisayar adı (token açıklaması için).</param>
public sealed record AgentTokenRequest(Guid UserId, Guid AgentDeviceId, string MachineName);
