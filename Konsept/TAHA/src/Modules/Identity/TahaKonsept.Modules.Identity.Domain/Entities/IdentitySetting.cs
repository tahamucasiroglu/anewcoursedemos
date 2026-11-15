namespace TahaKonsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Identity modülünün sunucu tarafı ayarı (anahtar → metin değer; tablo <c>identity.settings</c>). Anahtarlar
/// <see cref="Settings.IdentitySettingKeys"/>'te: yeni kayıtlara izin (<c>registration.open</c>) ve
/// ileride eklenecek diğer anahtarlar.
/// Davranış: <see cref="Update"/>.
/// </summary>
public class IdentitySetting
{
    /// <summary>EF için boş oluşturucu.</summary>
    protected IdentitySetting()
    {
    }

    /// <summary>Yeni ayar oluşturur.</summary>
    /// <param name="key">Anahtar.</param>
    /// <param name="value">Değer.</param>
    /// <param name="nowUtc">Şu an (UTC).</param>
    public IdentitySetting(string key, string value, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        Value = value ?? string.Empty;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Anahtar (birincil anahtar).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Değer.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Son güncelleme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Değeri değiştirir.</summary>
    /// <param name="value">Yeni değer.</param>
    /// <param name="nowUtc">Şu an (UTC).</param>
    public void Update(string value, DateTimeOffset nowUtc)
    {
        Value = value ?? string.Empty;
        UpdatedAtUtc = nowUtc;
    }
}
