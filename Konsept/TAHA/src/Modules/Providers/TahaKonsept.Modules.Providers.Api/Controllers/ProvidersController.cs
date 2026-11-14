using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Api.Controllers;

/// <summary>
/// Sağlayıcı uçları (<c>api/providers</c>): model kataloğu, API anahtarları (listele / kaydet / sil / test), aylık harcama,
/// bütçe ayarı. Tüm iş servislerde; aksiyonlar tek satır.
/// </summary>
[Route("api/providers")]
public sealed class ProvidersController : ApiControllerBase
{
    private readonly IModelCatalog _catalog;
    private readonly IApiKeyService _keys;
    private readonly IUsageService _usage;
    private readonly IBudgetService _budget;

    /// <summary>Kurucu.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    /// <param name="keys">Anahtar servisi.</param>
    /// <param name="usage">Harcama servisi.</param>
    /// <param name="budget">Bütçe servisi.</param>
    public ProvidersController(IModelCatalog catalog, IApiKeyService keys, IUsageService usage, IBudgetService budget)
    {
        _catalog = catalog;
        _keys = keys;
        _usage = usage;
        _budget = budget;
    }

    /// <summary>Model kataloğu (<c>?capability=Text|Image|Speech|VideoEngine</c>, <c>?runsOn=Server|Agent</c>).</summary>
    /// <param name="capability">Yetenek süzgeci.</param>
    /// <param name="runsOn">Çalışma yeri süzgeci.</param>
    [HttpGet("models")]
    public IActionResult Models([FromQuery] ModelCapability? capability, [FromQuery] ModelRunsOn? runsOn) =>
        ToActionResult(_catalog.Query(capability, runsOn));

    /// <summary>Tüm online platformlar ve anahtar durumları (yalnızca son 4 hane).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet("keys")]
    public async Task<IActionResult> Keys(CancellationToken ct) => ToActionResult(await _keys.ListAsync(ct));

    /// <summary>Anahtarı ekler / değiştirir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut("keys")]
    public async Task<IActionResult> SaveKey([FromBody] SaveApiKeyRequest request, CancellationToken ct) =>
        ToActionResult(await _keys.SaveAsync(request, ct));

    /// <summary>Anahtarı siler.</summary>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete("keys/{platform}")]
    public async Task<IActionResult> DeleteKey(string platform, CancellationToken ct) =>
        ToActionResult(await _keys.DeleteAsync(platform, ct));

    /// <summary>Anahtarı ücretsiz bir çağrıyla doğrular.</summary>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost("keys/{platform}/test")]
    public async Task<IActionResult> TestKey(string platform, CancellationToken ct) =>
        ToActionResult(await _keys.TestAsync(platform, ct));

    /// <summary>Bu ayın harcaması (<c>{ totalUsd, limitUsd, month, … }</c>).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet("usage/monthly")]
    public async Task<IActionResult> MonthlyUsage(CancellationToken ct) => ToActionResult(await _usage.GetMonthlyAsync(ct));

    /// <summary>Bütçe ayarı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet("budget")]
    public async Task<IActionResult> Budget(CancellationToken ct) => ToActionResult(await _budget.GetAsync(ct));

    /// <summary>Bütçe ayarını kaydeder.</summary>
    /// <param name="settings">Ayar.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut("budget")]
    public async Task<IActionResult> SaveBudget([FromBody] BudgetSettings settings, CancellationToken ct) =>
        ToActionResult(await _budget.SaveAsync(settings, ct));
}
