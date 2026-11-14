using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Alt "Günlük" şeridinin veri kaynağı. Beklenen API rotası:
/// <c>GET api/diagnostics/activity-logs/recent?afterId=&amp;take=50</c> → <c>[{ id, level, message, source, createdAt }]</c>.
/// </summary>
public sealed class GunlukController : WebControllerBase
{
    /// <summary>Son işlem kayıtları; <paramref name="afterId"/> verilirse yalnızca ondan yenileri.</summary>
    /// <param name="afterId">Son görülen kayıt kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Son(string? afterId, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>(ApiUrl("api/diagnostics/activity-logs/recent", ("afterId", afterId), ("take", 50)), ct));
}
