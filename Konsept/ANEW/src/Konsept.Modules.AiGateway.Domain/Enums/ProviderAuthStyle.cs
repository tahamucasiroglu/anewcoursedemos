namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Sağlayıcının key'i hangi başlıkta beklediği (seed verisinde).</summary>
public enum ProviderAuthStyle
{
    /// <summary><c>Authorization: Bearer &lt;key&gt;</c>.</summary>
    Bearer,

    /// <summary>Anthropic biçimi: <c>x-api-key</c> + <c>anthropic-version</c>.</summary>
    Anthropic,
}
