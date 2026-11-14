using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Üst bardaki aylık harcama rozeti. Beklenen API rotası:
/// <c>GET api/providers/usage/monthly</c> → <c>{ totalUsd, limitUsd, month }</c> (LLM + ses + görsel).
/// </summary>
public sealed class HarcamaController : WebControllerBase
{
    /// <summary>Bu ayın toplam harcaması.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Aylik(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/providers/usage/monthly", ct));
}
