namespace Konsept.Web.Models;

/// <summary>
/// Giriş ve kayıt formlarının ortak görünüm durumu: alan hataları (alan adına göre), genel mesaj (kalan hak, kilit, çok
/// fazla deneme, sunucuya ulaşılamıyor), bilgi notu (oturum sona erdi) ve güvenli dönüş adresi. Şifre hiçbir zaman forma
/// geri yazılmaz.
/// </summary>
public abstract class AuthForm
{
    /// <summary>Gets or sets e-posta.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets şifre (yalnızca gönderimde; görünüme geri yazılmaz).</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets dönüş adresi (yalnızca site içi kullanılır).</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Gets alan hataları (alan adı → Türkçe mesaj).</summary>
    public Dictionary<string, string> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets genel hata mesajı.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets bilgi notu (ör. "Oturumunuz sona erdi").</summary>
    public string? Notice { get; set; }

    /// <summary>Gets alanların ekrandaki sırası (ilk hatalı alana odak için).</summary>
    public abstract IReadOnlyList<string> FieldOrder { get; }

    /// <summary>Gets ilk hatalı alanın adı; yoksa <c>null</c>.</summary>
    public string? FirstInvalidField => FieldOrder.FirstOrDefault(Errors.ContainsKey);

    /// <summary>Alanın hata mesajı.</summary>
    /// <param name="field">Alan adı.</param>
    /// <returns>Mesaj ya da <c>null</c>.</returns>
    public string? ErrorFor(string field) => Errors.TryGetValue(field, out var message) ? message : null;
}
