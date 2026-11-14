namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Seslendirme yazılışının kelime sayımı (boşlukla ayrılmış parçalar) — konsept tarafının hedef kelime hesabıyla aynı ölçü.
/// Önemli metot: <see cref="Count"/>.
/// </summary>
public static class NarrationWords
{
    /// <summary>Kelime sayısı.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Sayı.</returns>
    public static int Count(string? text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
