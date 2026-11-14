namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş önizleme isteği (<c>POST api/production/pronunciations/preview</c>): metin normalize edilir ve kullanıcının ses ayarıyla seslendirilir.
/// </summary>
public sealed record PronunciationPreviewRequest
{
    /// <summary>Metin (yazılış ya da okunuş).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Önce okunuş düzeltmesi (sözlük + sayı/tarih ...) uygulansın mı.</summary>
    public bool Normalize { get; init; } = true;
}
