using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Providers.Domain.Entities;

/// <summary>
/// Kullanıcının bir platform için kaydettiği API anahtarı. Anahtar yalnızca şifreli (<see cref="ProtectedKey"/>, AES-256-GCM)
/// ve son 4 hane maskesi (<see cref="Last4"/>) olarak saklanır. Kullanıcı + platform başına tek kayıt.
/// Önemli üyeler: <see cref="Create"/>, <see cref="Replace"/>, <see cref="RecordTest"/>.
/// </summary>
public sealed class ProviderApiKey : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için.</summary>
    private ProviderApiKey()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Platform kimliği (ör. <c>anthropic</c>, <c>azure-speech</c>).</summary>
    public string Platform { get; private set; } = string.Empty;

    /// <summary>Şifreli anahtar (<c>ISecretProtector.Protect</c> çıktısı).</summary>
    public string ProtectedKey { get; private set; } = string.Empty;

    /// <summary>Anahtarın son 4 hanesi (gösterim için).</summary>
    public string Last4 { get; private set; } = string.Empty;

    /// <summary>Bölge (yalnızca Azure Speech).</summary>
    public string? Region { get; private set; }

    /// <summary>Son test sonucu (test edilmediyse null).</summary>
    public bool? LastTestSucceeded { get; private set; }

    /// <summary>Son test zamanı (UTC).</summary>
    public DateTimeOffset? LastTestedAtUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni anahtar kaydı oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform kimliği (küçük harf).</param>
    /// <param name="protectedKey">Şifreli anahtar.</param>
    /// <param name="last4">Son 4 hane.</param>
    /// <param name="region">Bölge (opsiyonel).</param>
    public static ProviderApiKey Create(Guid userId, string platform, string protectedKey, string last4, string? region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        var entity = new ProviderApiKey { UserId = userId, Platform = platform };
        entity.Replace(protectedKey, last4, region);
        return entity;
    }

    /// <summary>Anahtarı değiştirir; önceki test sonucu sıfırlanır.</summary>
    /// <param name="protectedKey">Yeni şifreli anahtar.</param>
    /// <param name="last4">Son 4 hane.</param>
    /// <param name="region">Bölge (opsiyonel).</param>
    public void Replace(string protectedKey, string last4, string? region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedKey);
        ProtectedKey = protectedKey;
        Last4 = last4 ?? string.Empty;
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        LastTestSucceeded = null;
        LastTestedAtUtc = null;
    }

    /// <summary>Doğrulama testinin sonucunu kaydeder.</summary>
    /// <param name="succeeded">Başarılı mı.</param>
    /// <param name="testedAtUtc">Test zamanı.</param>
    public void RecordTest(bool succeeded, DateTimeOffset testedAtUtc)
    {
        LastTestSucceeded = succeeded;
        LastTestedAtUtc = testedAtUtc;
    }
}
