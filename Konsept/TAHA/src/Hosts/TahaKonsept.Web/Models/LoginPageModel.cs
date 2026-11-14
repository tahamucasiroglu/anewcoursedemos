namespace TahaKonsept.Web.Models;

/// <summary>Giriş sayfasının görünüm modeli: girişten sonra dönülecek <see cref="ReturnUrl"/>.</summary>
public sealed record LoginPageModel
{
    /// <summary>Girişten sonra yönlendirilecek yerel adres.</summary>
    public string? ReturnUrl { get; init; }
}
