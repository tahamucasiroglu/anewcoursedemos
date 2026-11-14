using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Providers.Domain.Entities;

/// <summary>
/// Ücretlendirilebilir her online çağrının kaydı (LLM, online TTS, online görsel). Aylık harcama bu tablodan toplanır.
/// Maliyet çağrı anındaki fiyatla hesaplanıp <see cref="CostUsd"/>'e yazılır; sonradan fiyat değişse de geçmiş değişmez.
/// Önemli üyeler: <see cref="Create"/>.
/// </summary>
public sealed class CallLog : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>Hata mesajının saklanan en fazla uzunluğu.</summary>
    public const int MaxErrorLength = 1000;

    /// <summary>EF Core için.</summary>
    private CallLog()
    {
    }

    /// <summary>Çağrıyı yapan kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Platform kimliği.</summary>
    public string Platform { get; private set; } = string.Empty;

    /// <summary>Katalog anahtarı.</summary>
    public string ModelKey { get; private set; } = string.Empty;

    /// <summary>Sağlayıcıya gönderilen model kimliği.</summary>
    public string ProviderModelId { get; private set; } = string.Empty;

    /// <summary>Amaç (ör. <c>concept</c>, <c>scene-code</c>, <c>topics</c>, <c>key-test</c>).</summary>
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>Önbellek dışı giriş token'ı.</summary>
    public long InputTokens { get; private set; }

    /// <summary>Çıkış token'ı.</summary>
    public long OutputTokens { get; private set; }

    /// <summary>Önbellekten okunan giriş token'ı.</summary>
    public long CacheReadTokens { get; private set; }

    /// <summary>Önbelleğe yazılan giriş token'ı.</summary>
    public long CacheWriteTokens { get; private set; }

    /// <summary>Token dışı birim miktarı (görsel adedi, bin karakter, dakika, web araması).</summary>
    public decimal Units { get; private set; }

    /// <summary>Birim türü (<c>image</c>, <c>kchars</c>, <c>minute</c>, <c>search</c>; yoksa null).</summary>
    public string? UnitKind { get; private set; }

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; private set; }

    /// <summary>Süre (ms).</summary>
    public long DurationMs { get; private set; }

    /// <summary>Başarılı mı.</summary>
    public bool Success { get; private set; }

    /// <summary>Hata kodu (başarısızsa).</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Hata mesajı (kısaltılmış).</summary>
    public string? ErrorMessage { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni çağrı kaydı oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform.</param>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <param name="providerModelId">Sağlayıcı model kimliği.</param>
    /// <param name="purpose">Amaç.</param>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    /// <param name="cacheReadTokens">Önbellek okuma token'ı.</param>
    /// <param name="cacheWriteTokens">Önbellek yazma token'ı.</param>
    /// <param name="units">Birim miktarı.</param>
    /// <param name="unitKind">Birim türü.</param>
    /// <param name="costUsd">Maliyet.</param>
    /// <param name="durationMs">Süre.</param>
    /// <param name="success">Başarılı mı.</param>
    /// <param name="errorCode">Hata kodu.</param>
    /// <param name="errorMessage">Hata mesajı.</param>
    public static CallLog Create(
        Guid userId,
        string platform,
        string modelKey,
        string providerModelId,
        string purpose,
        long inputTokens,
        long outputTokens,
        long cacheReadTokens,
        long cacheWriteTokens,
        decimal units,
        string? unitKind,
        decimal costUsd,
        long durationMs,
        bool success,
        string? errorCode,
        string? errorMessage) =>
        new()
        {
            UserId = userId,
            Platform = platform ?? string.Empty,
            ModelKey = modelKey ?? string.Empty,
            ProviderModelId = providerModelId ?? string.Empty,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? "unspecified" : purpose,
            InputTokens = Math.Max(0, inputTokens),
            OutputTokens = Math.Max(0, outputTokens),
            CacheReadTokens = Math.Max(0, cacheReadTokens),
            CacheWriteTokens = Math.Max(0, cacheWriteTokens),
            Units = Math.Max(0, units),
            UnitKind = unitKind,
            CostUsd = Math.Max(0, costUsd),
            DurationMs = Math.Max(0, durationMs),
            Success = success,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage is { Length: > MaxErrorLength } ? errorMessage[..MaxErrorLength] : errorMessage,
        };
}
