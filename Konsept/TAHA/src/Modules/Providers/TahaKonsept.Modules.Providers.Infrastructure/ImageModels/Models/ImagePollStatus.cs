namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Models;

/// <summary>
/// Asenkron görsel işinin (BFL, Replicate) bir durum sorgusundaki hâli: sürüyor, tamamlandı (sonuç URL'leri) ya da başarısız
/// (hata kodu + mesaj). Fabrika üyeleri: <see cref="Pending"/>, <see cref="Done"/>, <see cref="Fail"/>.
/// </summary>
/// <param name="Completed">İş başarıyla bitti mi.</param>
/// <param name="FailureCode">Başarısızsa <c>providers.*</c> hata kodu (null = başarısız değil).</param>
/// <param name="FailureMessage">Başarısızlık mesajı.</param>
/// <param name="ImageUrls">Tamamlandıysa görsel URL'leri.</param>
public sealed record ImagePollStatus(bool Completed, string? FailureCode, string? FailureMessage, IReadOnlyList<string> ImageUrls)
{
    /// <summary>İş sürüyor.</summary>
    public static ImagePollStatus Pending { get; } = new(false, null, null, []);

    /// <summary>İş tamamlandı.</summary>
    /// <param name="urls">Görsel URL'leri.</param>
    public static ImagePollStatus Done(IReadOnlyList<string> urls) => new(true, null, null, urls);

    /// <summary>İş başarısız.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    public static ImagePollStatus Fail(string code, string message) => new(false, code, message, []);
}
