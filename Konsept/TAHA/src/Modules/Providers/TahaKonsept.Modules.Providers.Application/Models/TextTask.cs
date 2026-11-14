using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Diğer modüllerin <c>IProviderGateway</c>'e verdiği metin işi. <see cref="ModelKey"/> katalog anahtarıdır
/// (ör. <c>claude-sonnet-5-5</c>); OpenRouter için <c>openrouter:&lt;sağlayıcı/model&gt;</c> biçimi (ör. <c>openrouter:anthropic/claude-sonnet-5.5</c>).
/// <see cref="MaxOutputTokens"/> 0 ise model sınırı ile 16000'in küçüğü kullanılır; 16000 üstünde akış (streaming) açılır.
/// </summary>
public sealed record TextTask
{
    /// <summary>Çağrıyı yapan kullanıcı (anahtar ve harcama bu kullanıcıya göre).</summary>
    public Guid UserId { get; init; }

    /// <summary>Katalog anahtarı.</summary>
    public string ModelKey { get; init; } = string.Empty;

    /// <summary>Efor (null = modelin varsayılanı).</summary>
    public EffortLevel? Effort { get; init; }

    /// <summary>Sistem promptu (sabit içerik; önbelleğe uygun).</summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>Kullanıcı promptu (değişken içerik).</summary>
    public string UserPrompt { get; init; } = string.Empty;

    /// <summary>En fazla çıkış token'ı (0 = varsayılan; video kodu için 64000'e kadar).</summary>
    public int MaxOutputTokens { get; init; }

    /// <summary>Sunucu tarafı web araması (gerçek bilgi / fact-sheet). Desteklemeyen modelde yok sayılır.</summary>
    public bool UseWebSearch { get; init; }

    /// <summary>Amaç (CallLog'da görünür; ör. <c>concept</c>, <c>scene-code</c>, <c>topics</c>, <c>fact-sheet</c>).</summary>
    public string Purpose { get; init; } = string.Empty;

    /// <summary>Sistem promptu önbelleğe alınsın mı (Anthropic <c>cache_control</c>; OpenAI-uyumlularda otomatik).</summary>
    public bool CacheSystemPrompt { get; init; } = true;
}
