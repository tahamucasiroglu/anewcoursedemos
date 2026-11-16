using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Domain.Entities;

/// <summary>
/// Katalogdaki online sağlayıcı (spec 0004 Req 1): kararlı kimlik, görünen ad ve key doğrulamasının maliyetsiz ucu (yoksa
/// <c>null</c> → "Doğrulanmadı"). Değerler seed verisindedir.
/// </summary>
public sealed class ProviderEntry
{
    /// <summary>Gets kararlı kimlik (ör. <c>anthropic</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Gets görünen ad (ör. <c>Anthropic</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Gets key doğrulaması için token harcamayan GET ucu; yoksa <c>null</c>.</summary>
    public Uri? ValidationUrl { get; init; }

    /// <summary>Gets key'in gönderildiği başlık biçimi.</summary>
    public ProviderAuthStyle AuthStyle { get; init; }
}
