namespace TahaKonsept.Web.Models.Api;

/// <summary>API'ye token yenileme isteği (<c>POST api/identity/refresh</c>).</summary>
public sealed record IdentityRefreshRequest
{
    /// <summary>Mevcut refresh token.</summary>
    public string RefreshToken { get; init; } = string.Empty;
}
