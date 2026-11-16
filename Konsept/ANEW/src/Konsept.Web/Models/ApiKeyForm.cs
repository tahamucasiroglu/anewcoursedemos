namespace Konsept.Web.Models;

/// <summary>Ayarlar'daki key formu (Ajax JSON gövdesi; spec 0004 Req 7). Key yalnızca Api'ye iletilir, Web'de saklanmaz.</summary>
/// <param name="ApiKey">Girilen key.</param>
public sealed record ApiKeyForm(string? ApiKey);
