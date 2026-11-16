using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Kapının başarılı sonucu: metin (ya da görseller), sonuç (<see cref="CallOutcome.Success"/> ya da yarım kaldı —
/// <see cref="CallOutcome.Truncated"/>, çağıran özellik karar verir, Req 5), kullanılan efor ve çıktı sınırı, token sayıları, son
/// denemenin maliyeti, tahmini mi ve toplam deneme sayısı.
/// </summary>
public sealed record AiCallResult
{
    /// <summary>Gets model.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets metin.</summary>
    public string? Text { get; init; }

    /// <summary>Gets görseller (base64).</summary>
    public IReadOnlyList<string> Images { get; init; } = [];

    /// <summary>Gets sonuç.</summary>
    public CallOutcome Outcome { get; init; }

    /// <summary>Gets kullanılan efor.</summary>
    public string? Effort { get; init; }

    /// <summary>Gets sağlayıcıya giden çıktı sınırı.</summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>Gets giriş token.</summary>
    public int? InputTokens { get; init; }

    /// <summary>Gets çıkış token.</summary>
    public int? OutputTokens { get; init; }

    /// <summary>Gets maliyet (USD, yuvarlanmamış).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Gets a value indicating whether maliyet tahminidir.</summary>
    public bool IsEstimated { get; init; }

    /// <summary>Gets deneme sayısı.</summary>
    public int Attempts { get; init; }
}
