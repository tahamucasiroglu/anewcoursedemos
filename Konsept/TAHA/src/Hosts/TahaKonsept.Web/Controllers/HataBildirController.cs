using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Üst bardaki "Hata bildir" penceresinin gönderim ucu. API rotası: <c>POST api/diagnostics/error-reports</c>
/// (<c>{ source: "Web", message, details, pageUrl, userAgent, appVersion }</c>) → yönetim paneli "Hata bildirimleri".
/// Pencere: <c>wwwroot/js/core/error-report.js</c> (TK.errorReport).
/// </summary>
public sealed class HataBildirController : WebControllerBase
{
    /// <summary>Hata bildirimini gönderir.</summary>
    /// <param name="body">Bildirim.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Gonder([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/diagnostics/error-reports", body, ct));
}
