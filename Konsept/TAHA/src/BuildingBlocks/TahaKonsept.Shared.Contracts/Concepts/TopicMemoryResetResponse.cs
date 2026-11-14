namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konu hafızası sıfırlama yanıtı (<c>POST api/concepts/topic-memory/reset[?all=true]</c>): silinen kayıt sayısı.
/// Varsayılan sıfırlama yalnız öneri/beğenilmeyen kayıtları siler; <c>all=true</c> üretilen ve silinen konseptlerin izlerini de siler.
/// </summary>
public sealed record TopicMemoryResetResponse
{
    /// <summary>Silinen hafıza kaydı sayısı.</summary>
    public int Removed { get; init; }
}
