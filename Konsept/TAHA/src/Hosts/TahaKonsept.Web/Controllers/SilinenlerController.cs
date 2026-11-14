using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Silinenler sayfası (sonsuza kadar saklanır; buradan silinen tamamen silinir) ve Ajax uçları. Beklenen API rotaları:
/// <c>GET api/concepts/deleted</c>, <c>POST api/concepts/{id}/restore</c>, <c>DELETE api/concepts/{id}/purge</c>.
/// </summary>
public sealed class SilinenlerController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Silinen konseptler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Liste(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/concepts/deleted", ct));

    /// <summary>Konsepti geri alır.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> GeriAl(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/concepts/{Seg(id)}/restore", new { }, ct));

    /// <summary>Konsepti kalıcı olarak siler.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> KaliciSil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/concepts/{Seg(id)}/purge", ct));
}
