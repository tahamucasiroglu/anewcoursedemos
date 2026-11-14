namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş kaydı ekle/güncelle/onayla (<c>POST api/production/pronunciations</c>). <see cref="Id"/> verilirse güncellenir;
/// <see cref="IsSuggestion"/> <c>false</c> ise kayıt onaylı olur. Aynı yazılış varsa (büyük/küçük harf duyarsız) o kayıt güncellenir.
/// </summary>
public sealed record SavePronunciationRequest
{
    /// <summary>Güncellenecek kayıt (yeni için <c>null</c>).</summary>
    public Guid? Id { get; init; }

    /// <summary>Yazılış.</summary>
    public string Written { get; init; } = string.Empty;

    /// <summary>Okunuş.</summary>
    public string Spoken { get; init; } = string.Empty;

    /// <summary>Öneri olarak mı kaydedilsin (varsayılan: onaylı).</summary>
    public bool IsSuggestion { get; init; }
}
