using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Profiles.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Profiles.Domain.Entities;

/// <summary>
/// Kanal profili aggregate'i: kanal adı, video dili, biçim, hedef süre, kanal içeriği ve opsiyonel ton/izleyici/anlatım.
/// Kullanıcı başına tek aktif profil kuralını uygulama servisi korur (<see cref="Activate"/> / <see cref="Deactivate"/>).
/// Oluşturma: <see cref="Create"/>; düzenleme: <see cref="Update"/>.
/// </summary>
public sealed class Profile : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private Profile()
    {
    }

    private Profile(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Kanal adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Video dili.</summary>
    public string Language { get; private set; } = Constants.ProfileLimits.DefaultLanguage;

    /// <summary>Video biçimi.</summary>
    public VideoFormat Format { get; private set; } = VideoFormat.Vertical;

    /// <summary>Hedef süre (saniye).</summary>
    public int TargetDurationSec { get; private set; } = 60;

    /// <summary>Kanal içeriği.</summary>
    public string ChannelDescription { get; private set; } = string.Empty;

    /// <summary>Varsayılan ton.</summary>
    public string? DefaultTone { get; private set; }

    /// <summary>Varsayılan hedef izleyici.</summary>
    public string? DefaultAudience { get; private set; }

    /// <summary>Varsayılan anlatım biçimi.</summary>
    public string? DefaultNarrativeStyle { get; private set; }

    /// <summary>Kaynak şablon anahtarı.</summary>
    public string? TemplateKey { get; private set; }

    /// <summary>Aktif profil mi.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Oluşturulma anı (UTC; kayıtta otomatik).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Son güncellenme anı (UTC; kayıtta otomatik).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni (pasif) profil oluşturur.</summary>
    /// <param name="userId">Sahip kullanıcı.</param>
    /// <param name="details">Alanlar.</param>
    /// <returns>Profil.</returns>
    public static Profile Create(Guid userId, ProfileDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var profile = new Profile(userId);
        profile.Update(details);
        return profile;
    }

    /// <summary>Düzenlenebilir alanları günceller (değerler normalleştirilir).</summary>
    /// <param name="details">Yeni alanlar.</param>
    public void Update(ProfileDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var d = details.Normalize();
        Name = d.Name;
        Language = d.Language;
        Format = d.Format;
        TargetDurationSec = d.TargetDurationSec;
        ChannelDescription = d.ChannelDescription;
        DefaultTone = d.DefaultTone;
        DefaultAudience = d.DefaultAudience;
        DefaultNarrativeStyle = d.DefaultNarrativeStyle;
        TemplateKey = d.TemplateKey;
    }

    /// <summary>Profili aktif yapar.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Profili pasif yapar.</summary>
    public void Deactivate() => IsActive = false;
}
