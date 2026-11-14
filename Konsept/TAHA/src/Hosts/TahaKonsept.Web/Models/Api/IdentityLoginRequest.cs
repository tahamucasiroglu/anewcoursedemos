namespace TahaKonsept.Web.Models.Api;

/// <summary>API'ye giriş isteği (<c>POST api/identity/login</c>): kullanıcı adı ve parola.</summary>
public sealed record IdentityLoginRequest
{
    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola.</summary>
    public string Password { get; init; } = string.Empty;
}
