namespace TahaKonsept.Web.Models.Api;

/// <summary>İlk kurulumda tek yönetici hesabını oluşturma isteği (<c>POST api/identity/setup</c>).</summary>
public sealed record IdentitySetupRequest
{
    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola.</summary>
    public string Password { get; init; } = string.Empty;
}
