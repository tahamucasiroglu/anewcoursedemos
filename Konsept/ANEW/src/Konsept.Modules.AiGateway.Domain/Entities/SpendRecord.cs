using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Domain.Entities;

/// <summary>
/// Sağlayıcıya ulaşan tek bir denemenin harcama kaydı (spec 0013 Req 11–14, BR-12). Maliyet çağrı anındaki katalog fiyatıyla
/// (ya da sağlayıcının bildirdiğiyle) USD olarak, yuvarlanmadan saklanır; katalog sonradan değişse de kayıt değişmez. Key silinse
/// de kayıt kalır.
/// </summary>
public sealed class SpendRecord
{
    /// <summary>Gets kimlik (UUIDv7).</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi (BR-1).</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets deneme zamanı (UTC).</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Gets sağlayıcı kimliği.</summary>
    public required string ProviderId { get; init; }

    /// <summary>Gets model kimliği.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets efor seviyesi; yoksa <c>null</c>.</summary>
    public string? Effort { get; init; }

    /// <summary>Gets amaç.</summary>
    public CallPurpose Purpose { get; init; }

    /// <summary>Gets ilgili profil; yoksa <c>null</c>.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Gets giriş token sayısı (bildirilmediyse tahmin; hata yanıtında <c>null</c>).</summary>
    public int? InputTokens { get; init; }

    /// <summary>Gets çıkış token sayısı; bilinmiyorsa <c>null</c>.</summary>
    public int? OutputTokens { get; init; }

    /// <summary>Gets görsel adedi (görsel çağrısı).</summary>
    public int? ImageCount { get; init; }

    /// <summary>Gets sonuç.</summary>
    public CallOutcome Outcome { get; init; }

    /// <summary>Gets maliyet (USD, yuvarlanmamış).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Gets a value indicating whether maliyet tahminidir (token sayısı bildirilmedi, Req 13).</summary>
    public bool IsEstimated { get; init; }
}
