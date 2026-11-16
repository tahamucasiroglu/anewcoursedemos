using Konsept.Modules.AiGateway.Application.Enums;

namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Tek denemenin sonucu: tür, hata durum kodu, metin ya da görseller, bildirilen token sayıları (yoksa <c>null</c>), yarım kaldı
/// mı ve sağlayıcının bildirdiği maliyet (OpenRouter). Sağlayıcının ham yanıt gövdesi hiçbir alanda taşınmaz (Req 9).
/// </summary>
public sealed record ProviderAttempt
{
    /// <summary>Gets tür.</summary>
    public ProviderAttemptKind Kind { get; init; }

    /// <summary>Gets HTTP hata kodu.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Gets yanıt metni.</summary>
    public string? Text { get; init; }

    /// <summary>Gets üretilen görseller (base64).</summary>
    public IReadOnlyList<string> Images { get; init; } = [];

    /// <summary>Gets bildirilen giriş token.</summary>
    public int? InputTokens { get; init; }

    /// <summary>Gets bildirilen çıkış token.</summary>
    public int? OutputTokens { get; init; }

    /// <summary>Gets a value indicating whether çıktı sınıra takıldı.</summary>
    public bool Truncated { get; init; }

    /// <summary>Gets sağlayıcının bildirdiği maliyet (USD).</summary>
    public decimal? ReportedCostUsd { get; init; }

    /// <summary>Başarılı yanıt.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="inputTokens">Giriş token.</param>
    /// <param name="outputTokens">Çıkış token.</param>
    /// <param name="truncated">Yarım kaldı mı.</param>
    /// <returns>Sonuç.</returns>
    public static ProviderAttempt Completed(string? text, int? inputTokens, int? outputTokens, bool truncated = false) =>
        new() { Kind = ProviderAttemptKind.Completed, Text = text, InputTokens = inputTokens, OutputTokens = outputTokens, Truncated = truncated };

    /// <summary>HTTP hata yanıtı.</summary>
    /// <param name="statusCode">Durum kodu.</param>
    /// <returns>Sonuç.</returns>
    public static ProviderAttempt Failed(int statusCode) => new() { Kind = ProviderAttemptKind.HttpError, StatusCode = statusCode };

    /// <summary>Bağlantı kurulamadı.</summary>
    /// <returns>Sonuç.</returns>
    public static ProviderAttempt Unreachable() => new() { Kind = ProviderAttemptKind.ConnectionFailed };

    /// <summary>Gönderildi, yanıt alınamadı.</summary>
    /// <returns>Sonuç.</returns>
    public static ProviderAttempt Lost() => new() { Kind = ProviderAttemptKind.NoResponse };
}
