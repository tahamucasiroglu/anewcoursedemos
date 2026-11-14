using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;

namespace TahaKonsept.Web.Infrastructure.Auth.Base;

/// <summary>
/// <see cref="ITokenStore"/> ortak davranışı: token'ların <see cref="AuthenticationProperties"/> içine yazılması
/// (<see cref="WriteTokens"/>) ve oradan okunması (<see cref="ReadTokens"/>). Oturumun nerede tutulduğu somut sınıftadır.
/// </summary>
public abstract class TokenStoreBase : ITokenStore
{
    /// <inheritdoc />
    public abstract Task<TokenResponse?> GetAsync();

    /// <inheritdoc />
    public abstract Task SaveAsync(TokenResponse tokens, bool? isPersistent = null);

    /// <inheritdoc />
    public abstract Task ClearAsync();

    /// <summary>Token'ları kimlik doğrulama özelliklerine yazar.</summary>
    /// <param name="properties">Hedef özellikler.</param>
    /// <param name="tokens">Token'lar.</param>
    protected static void WriteTokens(AuthenticationProperties properties, TokenResponse tokens)
    {
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = AuthTokenNames.AccessToken, Value = tokens.AccessToken },
            new AuthenticationToken { Name = AuthTokenNames.RefreshToken, Value = tokens.RefreshToken },
            new AuthenticationToken { Name = AuthTokenNames.AccessTokenExpiresAt, Value = tokens.AccessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture) },
            new AuthenticationToken { Name = AuthTokenNames.RefreshTokenExpiresAt, Value = tokens.RefreshTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture) },
        ]);
    }

    /// <summary>Kimlik doğrulama özelliklerinden token'ları okur; erişim token'ı yoksa null.</summary>
    /// <param name="properties">Kaynak özellikler.</param>
    protected static TokenResponse? ReadTokens(AuthenticationProperties? properties)
    {
        var access = properties?.GetTokenValue(AuthTokenNames.AccessToken);
        if (properties is null || string.IsNullOrEmpty(access))
        {
            return null;
        }

        return new TokenResponse
        {
            AccessToken = access,
            RefreshToken = properties.GetTokenValue(AuthTokenNames.RefreshToken) ?? string.Empty,
            AccessTokenExpiresAtUtc = ParseDate(properties.GetTokenValue(AuthTokenNames.AccessTokenExpiresAt)),
            RefreshTokenExpiresAtUtc = ParseDate(properties.GetTokenValue(AuthTokenNames.RefreshTokenExpiresAt)),
        };
    }

    private static DateTimeOffset ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : DateTimeOffset.MinValue;
}
