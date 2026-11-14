using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Kullanıcı başına üretim ayarları aggregate'i. Ayarlar JSON olarak saklanır (<see cref="SettingsJson"/>; biçim
/// Shared.Contracts <c>ProductionSettingsDto</c>) — alan eklemek migration gerektirmez.
/// Oluşturma: <see cref="Create"/>; güncelleme: <see cref="Update"/>.
/// </summary>
public sealed class ProductionSettings : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private ProductionSettings()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Ayarlar (JSON).</summary>
    public string SettingsJson { get; private set; } = "{}";

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni ayar kaydı oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="settingsJson">Ayar JSON'u.</param>
    /// <returns>Kayıt.</returns>
    public static ProductionSettings Create(Guid userId, string settingsJson) =>
        new() { UserId = userId, SettingsJson = settingsJson };

    /// <summary>Ayarları değiştirir.</summary>
    /// <param name="settingsJson">Yeni ayar JSON'u.</param>
    public void Update(string settingsJson) => SettingsJson = settingsJson;
}
