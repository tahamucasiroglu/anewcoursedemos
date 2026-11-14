using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Enums;

namespace TahaKonsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Konu hafızası kaydı (profil başına): başlık, normalleştirilmiş anahtar kelimeler (<see cref="Keywords"/>; benzerlik için),
/// kısa özet karması (<see cref="Hash"/>; birebir tekrar için), tür (<see cref="TopicMemoryKind"/>) ve bağlı konsept.
/// Önerilen/üretilen/silinen/beğenilmeyen her konu kaydedilir; aynı konu tekrar önerilmez. Tür yalnız güçlenir (<see cref="Promote"/>),
/// geri alınan konsept için <see cref="Demote"/> kullanılır.
/// </summary>
public sealed class TopicMemoryEntry : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private TopicMemoryEntry()
    {
    }

    private TopicMemoryEntry(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Profil (null = profilsiz).</summary>
    public Guid? ProfileId { get; private set; }

    /// <summary>Başlık (görüntüleme ve prompt için).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Normalleştirilmiş anahtar kelimeler (boşlukla ayrılmış, sıralı).</summary>
    public string Keywords { get; private set; } = string.Empty;

    /// <summary>Anahtar kelimelerin kısa karması (birebir eşleşme).</summary>
    public string Hash { get; private set; } = string.Empty;

    /// <summary>Tür.</summary>
    public TopicMemoryKind Kind { get; private set; }

    /// <summary>Bağlı konsept.</summary>
    public Guid? ConceptId { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Son güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Negatif örnek mi (silinen / beğenilmeyen).</summary>
    public bool IsNegative => Kind is TopicMemoryKind.Deleted or TopicMemoryKind.Disliked;

    /// <summary>Yeni kayıt oluşturur.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="title">Başlık.</param>
    /// <param name="keywords">Normalleştirilmiş anahtar kelimeler.</param>
    /// <param name="hash">Karma.</param>
    /// <param name="kind">Tür.</param>
    /// <param name="conceptId">Bağlı konsept.</param>
    /// <returns>Kayıt.</returns>
    public static TopicMemoryEntry Create(Guid userId, Guid? profileId, string title, string keywords, string hash, TopicMemoryKind kind, Guid? conceptId = null)
    {
        var t = (title ?? string.Empty).Trim();
        var k = keywords ?? string.Empty;
        return new TopicMemoryEntry(userId)
        {
            ProfileId = profileId,
            Title = t.Length <= ConceptLimits.TitleMaxLength ? t : t[..ConceptLimits.TitleMaxLength],
            Keywords = k.Length <= ConceptLimits.KeywordsMaxLength ? k : k[..ConceptLimits.KeywordsMaxLength],
            Hash = hash ?? string.Empty,
            Kind = kind,
            ConceptId = conceptId,
        };
    }

    /// <summary>Türü güçlendirir (zayıf türe düşmez); konsept bağı verilirse yazılır.</summary>
    /// <param name="kind">Yeni tür.</param>
    /// <param name="conceptId">Bağlı konsept.</param>
    /// <returns>Değişti mi.</returns>
    public bool Promote(TopicMemoryKind kind, Guid? conceptId = null)
    {
        var changed = false;
        if (kind > Kind)
        {
            Kind = kind;
            changed = true;
        }

        if (conceptId is not null && ConceptId != conceptId)
        {
            ConceptId = conceptId;
            changed = true;
        }

        return changed;
    }

    /// <summary>Türü verilen değere indirir (ör. konsept geri alınınca Deleted → Generated).</summary>
    /// <param name="kind">Yeni tür.</param>
    public void Demote(TopicMemoryKind kind) => Kind = kind;
}
