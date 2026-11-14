namespace TahaKonsept.Web.Models;

/// <summary>Tarayıcıdan gelen ilk kurulum isteği (Ajax JSON): kullanıcı adı, parola ve tekrarı.</summary>
public sealed record SetupRequest
{
    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Parola tekrarı.</summary>
    public string PasswordConfirm { get; init; } = string.Empty;
}
