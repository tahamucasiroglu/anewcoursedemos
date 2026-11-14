namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin tamamı (<c>GET api/concepts/{id}</c>; Production <c>IConceptReader</c> da bunu döner): belge
/// (<see cref="Document"/>), durum, kaynak, kullanılan model, token ve maliyet, zaman damgaları ve düzeltme bağı.
/// </summary>
public sealed record ConceptDetailDto
{
    /// <summary>Konsept kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Üretildiği profil (kanal).</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Durum.</summary>
    public ConceptStatus Status { get; init; }

    /// <summary>Kaynak.</summary>
    public ConceptOrigin Origin { get; init; }

    /// <summary>Konsept belgesi (sahneler, kaynaklar, okunuşlar…).</summary>
    public ConceptDocument Document { get; init; } = new();

    /// <summary>Üreten modelin katalog anahtarı.</summary>
    public string ModelKey { get; init; } = string.Empty;

    /// <summary>Toplam giriş token'ı.</summary>
    public long InputTokens { get; init; }

    /// <summary>Toplam çıkış token'ı.</summary>
    public long OutputTokens { get; init; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Bu konseptin türetildiği konsept (düzeltme kopyasıysa).</summary>
    public Guid? RevisionOfId { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Son güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Onaylanma anı (UTC).</summary>
    public DateTimeOffset? ApprovedAt { get; init; }

    /// <summary>Silinme anı (UTC).</summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
