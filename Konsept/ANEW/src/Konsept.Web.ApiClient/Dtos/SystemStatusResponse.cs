namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// API'nin sistem durumu cevabı.
/// </summary>
/// <param name="Version">Ürün versiyonu.</param>
/// <param name="DatabaseAvailable">Veritabanı erişilebilir mi.</param>
/// <param name="Agent">Ajan durumu.</param>
/// <param name="LastEvent">Son Günlük olayı.</param>
public sealed record SystemStatusResponse(string Version, bool DatabaseAvailable, AgentStatusResponse Agent, SystemEventResponse? LastEvent);
