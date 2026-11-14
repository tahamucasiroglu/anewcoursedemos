namespace TahaKonsept.Web.Models;

/// <summary>
/// Başlangıç kontrol listesinin tamamı (<c>GET /Baslangic/Durum</c> cevabı): <see cref="Steps"/> (sabit sırada),
/// <see cref="Remaining"/> (bekleyen adım sayısı; üst menü rozeti) ve <see cref="IsComplete"/> (hepsi tamam mı).
/// </summary>
public sealed record OnboardingStatus
{
    /// <summary>Adımlar (<see cref="OnboardingStepKeys.All"/> sırasıyla).</summary>
    public IReadOnlyList<OnboardingStep> Steps { get; init; } = [];

    /// <summary>Durumu <see cref="OnboardingStepState.Waiting"/> olan adım sayısı.</summary>
    public int Remaining { get; init; }

    /// <summary>Tüm adımlar tamam mı.</summary>
    public bool IsComplete { get; init; }
}
