using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Downloads.Abstract;
using TahaKonsept.Web.Services.Onboarding.Abstract;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Başlangıç (karşılama) sayfası: ilk videoya giden adımların canlı kontrol listesi ve masaüstü programı indirme düğmesi.
/// İlk kurulumdan sonra buraya yönlendirilir. Uçlar: <c>GET /Baslangic</c> (sayfa; model <see cref="AgentReleaseInfo"/>),
/// <c>GET /Baslangic/Durum?yenile=true</c> (<see cref="OnboardingStatus"/>; üst menü rozeti de bunu kullanır).
/// Eşleme kodu onayı ve bilgisayar listesi Ayarlar uçlarını paylaşır (<c>/Ayarlar/CihazOnayla</c>, <c>/Ayarlar/Ajanlar</c>).
/// </summary>
public sealed class BaslangicController : WebControllerBase
{
    private readonly IAgentReleaseProvider _releases;
    private readonly IOnboardingService _onboarding;

    /// <summary>Servisleri alır.</summary>
    /// <param name="releases">Kurulum dosyası bilgisi.</param>
    /// <param name="onboarding">Kontrol listesi durumu.</param>
    public BaslangicController(IAgentReleaseProvider releases, IOnboardingService onboarding)
    {
        _releases = releases;
        _onboarding = onboarding;
    }

    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View(_releases.GetCurrent());

    /// <summary>Kontrol listesinin durumu.</summary>
    /// <param name="yenile">true ise önbellek atlanır.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public async Task<IActionResult> Durum(bool yenile, CancellationToken ct) => Ok(await _onboarding.GetStatusAsync(yenile, ct));
}
