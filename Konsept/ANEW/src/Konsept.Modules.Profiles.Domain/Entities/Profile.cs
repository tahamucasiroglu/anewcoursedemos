namespace Konsept.Modules.Profiles.Domain.Entities;

/// <summary>
/// Kullanıcının bir kanal profili (spec 0003 Req 1–2): kanal adı, video dili, biçim, hedef süre ve kanal içeriği. Sahibi tek bir
/// kullanıcıdır (BR-1); <see cref="NameKey"/> aynı kullanıcının profilleri arasında tekildir (tr-TR büyük/küçük harf duyarsız).
/// </summary>
public sealed class Profile
{
    /// <summary>Gets profil kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets kanal adı (kırpılmış).</summary>
    public required string Name { get; init; }

    /// <summary>Gets tekillik anahtarı (<see cref="Rules.ProfileRules.NameKey"/>).</summary>
    public required string NameKey { get; init; }

    /// <summary>Gets video dili (<c>tr</c> · <c>en</c>).</summary>
    public required string Language { get; init; }

    /// <summary>Gets biçim (<c>landscape</c> · <c>portrait</c>).</summary>
    public required string Format { get; init; }

    /// <summary>Gets hedef süre (saniye, 15–900).</summary>
    public int DurationSeconds { get; init; }

    /// <summary>Gets kanal içeriği (kırpılmış).</summary>
    public required string Content { get; init; }

    /// <summary>Gets oluşturulma zamanı (UTC; "ilk oluşturulan" kuralı, Req 9).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets son güncellenme zamanı (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
