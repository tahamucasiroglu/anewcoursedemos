using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Providers.Domain.Entities;

/// <summary>
/// Sürümlü model fiyatı (kullanıcının katalog fiyatını geçersiz kılması). Maliyet hesabında, <see cref="EffectiveFromUtc"/>'i
/// çağrı zamanından önce olan en yeni kayıt kullanılır; kayıt yoksa katalogdaki fiyat geçerlidir.
/// Önemli üyeler: <see cref="Create"/>.
/// </summary>
public sealed class ModelPrice : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için.</summary>
    private ModelPrice()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Katalog anahtarı.</summary>
    public string ModelKey { get; private set; } = string.Empty;

    /// <summary>Geçerlilik başlangıcı (UTC).</summary>
    public DateTimeOffset EffectiveFromUtc { get; private set; }

    /// <summary>Giriş, $/1M token.</summary>
    public decimal? InputPerMTok { get; private set; }

    /// <summary>Çıkış, $/1M token.</summary>
    public decimal? OutputPerMTok { get; private set; }

    /// <summary>Önbellek okuma, $/1M token.</summary>
    public decimal? CacheReadPerMTok { get; private set; }

    /// <summary>Önbellek yazma, $/1M token.</summary>
    public decimal? CacheWritePerMTok { get; private set; }

    /// <summary>Görsel başı ($).</summary>
    public decimal? PerImage { get; private set; }

    /// <summary>Bin karakter başı ($).</summary>
    public decimal? PerKChars { get; private set; }

    /// <summary>Dakika başı ($).</summary>
    public decimal? PerMinute { get; private set; }

    /// <summary>Not (kaynak, tarih).</summary>
    public string? Note { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni fiyat sürümü oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <param name="effectiveFromUtc">Geçerlilik başlangıcı.</param>
    /// <param name="inputPerMTok">Giriş fiyatı.</param>
    /// <param name="outputPerMTok">Çıkış fiyatı.</param>
    /// <param name="cacheReadPerMTok">Önbellek okuma fiyatı.</param>
    /// <param name="cacheWritePerMTok">Önbellek yazma fiyatı.</param>
    /// <param name="perImage">Görsel başı.</param>
    /// <param name="perKChars">Bin karakter başı.</param>
    /// <param name="perMinute">Dakika başı.</param>
    /// <param name="note">Not.</param>
    public static ModelPrice Create(
        Guid userId,
        string modelKey,
        DateTimeOffset effectiveFromUtc,
        decimal? inputPerMTok,
        decimal? outputPerMTok,
        decimal? cacheReadPerMTok,
        decimal? cacheWritePerMTok,
        decimal? perImage,
        decimal? perKChars,
        decimal? perMinute,
        string? note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelKey);
        return new ModelPrice
        {
            UserId = userId,
            ModelKey = modelKey,
            EffectiveFromUtc = effectiveFromUtc,
            InputPerMTok = inputPerMTok,
            OutputPerMTok = outputPerMTok,
            CacheReadPerMTok = cacheReadPerMTok,
            CacheWritePerMTok = cacheWritePerMTok,
            PerImage = perImage,
            PerKChars = perKChars,
            PerMinute = perMinute,
            Note = note,
        };
    }
}
