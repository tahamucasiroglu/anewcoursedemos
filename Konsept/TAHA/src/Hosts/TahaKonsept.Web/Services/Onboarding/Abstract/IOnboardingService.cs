using TahaKonsept.Web.Models;

namespace TahaKonsept.Web.Services.Onboarding.Abstract;

/// <summary>
/// Başlangıç (ilk videoya giden yol) kontrol listesinin canlı durumu: LLM anahtarı, masaüstü programı eşlemesi,
/// ilk profil, ilk konsept, ilk video. <see cref="GetStatusAsync"/> mevcut API uçlarından okur; üst menü rozeti için kısa süre önbellekler.
/// </summary>
public interface IOnboardingService
{
    /// <summary>Kontrol listesinin durumunu döner.</summary>
    /// <param name="refresh">true ise önbellek atlanır (Başlangıç sayfasının canlı yenilemesi).</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<OnboardingStatus> GetStatusAsync(bool refresh, CancellationToken ct = default);
}
