namespace TahaKonsept.Web.Models;

/// <summary>
/// Başlangıç durumunu hesaplamak için API'den toplanan ham bilgiler. Her alan null ise o bilgi alınamadı demektir
/// (adım <see cref="OnboardingStepState.Unknown"/> olur). Önemli alanlar: <see cref="LlmKeyPlatforms"/> (anahtarı kayıtlı
/// metin/LLM platformlarının görünen adları), <see cref="PairedAgents"/> / <see cref="OnlineAgents"/>, profil/konsept/video sayıları.
/// </summary>
public sealed record OnboardingFacts
{
    /// <summary>Anahtarı kayıtlı LLM platformlarının görünen adları.</summary>
    public IReadOnlyList<string>? LlmKeyPlatforms { get; init; }

    /// <summary>Eşlenmiş masaüstü programı sayısı.</summary>
    public int? PairedAgents { get; init; }

    /// <summary>Şu an bağlı (çevrimiçi) masaüstü programı sayısı.</summary>
    public int? OnlineAgents { get; init; }

    /// <summary>Kanal profili sayısı.</summary>
    public int? Profiles { get; init; }

    /// <summary>Silinmemiş konsept sayısı.</summary>
    public int? Concepts { get; init; }

    /// <summary>Üretilmiş video sayısı (en az bir tane olup olmadığı yeterli).</summary>
    public int? Videos { get; init; }
}
