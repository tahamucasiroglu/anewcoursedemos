namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// "Şunları düzelterek tekrar üret" (<c>POST api/concepts/{id}/revise</c>): talimat ve isteğe bağlı temel belge
/// (kaydedilmemiş düzenlemeler ya da "Bunu da düzenle" döngüsündeki aday; boşsa kayıtlı belge).
/// </summary>
public sealed record ReviseConceptRequest
{
    /// <summary>Düzeltme talimatı.</summary>
    public string Instruction { get; init; } = string.Empty;

    /// <summary>Temel alınacak belge (isteğe bağlı).</summary>
    public ConceptDocument? Document { get; init; }
}
