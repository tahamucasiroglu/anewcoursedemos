namespace TahaKonsept.Web.Models;

/// <summary>Başlangıç kontrol listesi adım anahtarları (sayfadaki <c>data-step</c> değerleriyle aynı); sıra = <see cref="All"/>.</summary>
public static class OnboardingStepKeys
{
    /// <summary>LLM sağlayıcı anahtarı girildi.</summary>
    public const string LlmKey = "llm-key";

    /// <summary>Masaüstü programı kuruldu ve eşlendi.</summary>
    public const string Agent = "agent";

    /// <summary>İlk kanal profili oluşturuldu.</summary>
    public const string Profile = "profile";

    /// <summary>İlk konsept üretildi.</summary>
    public const string Concept = "concept";

    /// <summary>İlk video üretildi.</summary>
    public const string Video = "video";

    /// <summary>Adımların sırası.</summary>
    public static IReadOnlyList<string> All { get; } = [LlmKey, Agent, Profile, Concept, Video];
}
