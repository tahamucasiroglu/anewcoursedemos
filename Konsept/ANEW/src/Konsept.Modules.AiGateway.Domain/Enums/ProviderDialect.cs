namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Sağlayıcının çağrı API biçimi (seed verisinde; istek gövdesi ve kullanım alanları buna göre kurulur).</summary>
public enum ProviderDialect
{
    /// <summary>Anthropic Messages API (<c>max_tokens</c>, <c>output_config.effort</c>, <c>stop_reason</c>).</summary>
    Anthropic,

    /// <summary>OpenAI Chat Completions (<c>max_completion_tokens</c>, <c>reasoning_effort</c>) ve Images.</summary>
    OpenAi,

    /// <summary>OpenRouter (OpenAI uyumlu; yanıtta bildirilen maliyet <c>usage.cost</c>).</summary>
    OpenRouter,

    /// <summary>Diğer OpenAI uyumlu uçlar (GLM, Kimi; <c>max_tokens</c>).</summary>
    Compatible,
}
