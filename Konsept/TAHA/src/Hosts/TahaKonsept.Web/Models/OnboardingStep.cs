namespace TahaKonsept.Web.Models;

/// <summary>
/// Başlangıç kontrol listesinin bir satırı: <see cref="Key"/> (sabit anahtar, <see cref="OnboardingStepKeys"/>),
/// <see cref="State"/> ve kullanıcıya gösterilecek kısa <see cref="Detail"/> (ör. "Anthropic (Claude) kayıtlı").
/// </summary>
public sealed record OnboardingStep
{
    /// <summary>Adım anahtarı (<see cref="OnboardingStepKeys"/>).</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Durum.</summary>
    public OnboardingStepState State { get; init; }

    /// <summary>Kısa açıklama (boş olabilir).</summary>
    public string Detail { get; init; } = string.Empty;
}
