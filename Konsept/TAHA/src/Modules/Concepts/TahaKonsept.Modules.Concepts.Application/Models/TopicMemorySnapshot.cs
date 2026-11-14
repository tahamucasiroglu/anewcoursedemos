namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Bir profilin konu hafızası (en yeni önce). Prompta sınırlı bir kısmı girer; tekrar elemesi tamamıyla yapılır.
/// </summary>
/// <param name="Items">Kayıtlar (en yeni önce).</param>
public sealed record TopicMemorySnapshot(IReadOnlyList<TopicMemoryItem> Items)
{
    /// <summary>Boş hafıza.</summary>
    public static TopicMemorySnapshot Empty { get; } = new([]);
}
