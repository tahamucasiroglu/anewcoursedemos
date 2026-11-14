namespace TahaKonsept.Web.Models;

/// <summary>Ajax ile yapılan giriş/kurulum sonrası tarayıcının gideceği adres.</summary>
public sealed record RedirectResponse
{
    /// <summary>Yönlendirilecek yerel adres.</summary>
    public string RedirectUrl { get; init; } = "/";
}
