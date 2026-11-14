using System.Text.Json.Nodes;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir metin modeline giden normalize edilmiş istek (gateway <see cref="TextTask"/>'ı model sınırlarına göre buna çevirir).
/// <see cref="JsonSchema"/> doluysa yapılandırılmış JSON çıktı istenir.
/// </summary>
public sealed record TextRequest
{
    /// <summary>Kullanıcı + amaç.</summary>
    public CallContext Context { get; init; } = new(Guid.Empty, string.Empty);

    /// <summary>Sistem promptu.</summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>Kullanıcı promptu.</summary>
    public string UserPrompt { get; init; } = string.Empty;

    /// <summary>En fazla çıkış token'ı.</summary>
    public int MaxOutputTokens { get; init; } = 4096;

    /// <summary>Efor (null = gönderilmez).</summary>
    public EffortLevel? Effort { get; init; }

    /// <summary>Web araması.</summary>
    public bool UseWebSearch { get; init; }

    /// <summary>Sistem promptunu önbelleğe al.</summary>
    public bool CacheSystemPrompt { get; init; } = true;

    /// <summary>Çıktı JSON şeması (null = serbest metin).</summary>
    public JsonObject? JsonSchema { get; init; }

    /// <summary>Şema adı (OpenAI <c>json_schema.name</c>).</summary>
    public string SchemaName { get; init; } = "response";

    /// <summary>Akış kullanılsın mı (uzun çıktılarda zaman aşımını önler).</summary>
    public bool Stream { get; init; }
}
