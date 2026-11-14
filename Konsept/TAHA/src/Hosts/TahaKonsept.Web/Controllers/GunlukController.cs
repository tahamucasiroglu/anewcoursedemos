using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Alt "Günlük" şeridinin uçları. API rotaları (Diagnostics modülü):
/// <c>GET api/diagnostics/activity-logs/recent?afterId=&amp;take=50</c> → <c>[{ id, level, source, message, details, durationMs, createdAt }]</c>
/// (eskiden yeniye) ve <c>POST api/diagnostics/activity-logs</c> (<c>{ level, source, message, details, durationMs }</c>) tarayıcı olayları için.
/// Gövdeler ham JSON olarak aktarılır (API'nin metin enum biçimi korunur).
/// </summary>
public sealed class GunlukController : WebControllerBase
{
    /// <summary>Son işlem kayıtları; <paramref name="afterId"/> verilirse yalnızca ondan yenileri.</summary>
    /// <param name="afterId">Son görülen kayıt kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Son(string? afterId, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>(ApiUrl("api/diagnostics/activity-logs/recent", ("afterId", afterId), ("take", 50)), ct));

    /// <summary>Tarayıcıda olan bir olayı (ör. önizleme açılamadı) günlüğe yazar.</summary>
    /// <param name="body"><c>{ level, source, message, details, durationMs }</c>.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Kaydet([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/diagnostics/activity-logs", body, ct));
}
