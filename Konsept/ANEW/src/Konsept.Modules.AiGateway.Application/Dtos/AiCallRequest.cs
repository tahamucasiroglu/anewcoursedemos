using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Çağrı kapısına istek (spec 0013): çağıran, model, amaç, varsa profil, efor (yoksa katalog varsayılanı), istenen çıktı sınırı
/// (katalog limitiyle kırpılır), sistem metni ve istem. Görsel modelinde <see cref="ImageCount"/> görsel üretilir.
/// </summary>
public sealed record AiCallRequest
{
    /// <summary>Gets çağıran.</summary>
    public required AiCaller Caller { get; init; }

    /// <summary>Gets katalog model kimliği.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets amaç.</summary>
    public CallPurpose Purpose { get; init; }

    /// <summary>Gets ilgili profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Gets efor; <c>null</c> → katalog varsayılanı.</summary>
    public string? Effort { get; init; }

    /// <summary>Gets istenen çıktı token sınırı; <c>null</c> ya da katalogdan büyükse katalog limiti.</summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>Gets sistem metni.</summary>
    public string? System { get; init; }

    /// <summary>Gets istem (görselde görsel tarifi).</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets görsel adedi (yalnızca görsel modeli).</summary>
    public int ImageCount { get; init; } = 1;
}
