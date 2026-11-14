using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Base;

namespace TahaKonsept.Web.Infrastructure.Auth.Concrete;

/// <summary>
/// Token'ları şifreli auth cookie'sinde tutan depo (BFF). Aynı istek içinde yenilenen token'lar
/// <c>HttpContext.Items</c>'ta da tutulur; böylece aynı istekteki sonraki API çağrıları yeni token'ı kullanır.
/// Cevap başlamışsa cookie yeniden yazılamaz; bu durumda uyarı günlüğe düşer (sonraki istekte yenileme önbellekten gelir).
/// </summary>
public sealed class CookieTokenStore : TokenStoreBase
{
    private const string ItemsKey = "TahaKonsept.Tokens";
    private readonly IHttpContextAccessor _accessor;
    private readonly IPrincipalFactory _principals;
    private readonly ILogger<CookieTokenStore> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="accessor">Geçerli HttpContext erişimi.</param>
    /// <param name="principals">Token'dan kimlik kuran fabrika.</param>
    /// <param name="logger">Günlükleyici.</param>
    public CookieTokenStore(IHttpContextAccessor accessor, IPrincipalFactory principals, ILogger<CookieTokenStore> logger)
    {
        _accessor = accessor;
        _principals = principals;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<TokenResponse?> GetAsync()
    {
        var context = _accessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (context.Items.TryGetValue(ItemsKey, out var cached))
        {
            return cached as TokenResponse;
        }

        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return result.Succeeded ? ReadTokens(result.Properties) : null;
    }

    /// <inheritdoc />
    public override async Task SaveAsync(TokenResponse tokens, bool? isPersistent = null)
    {
        var context = _accessor.HttpContext ?? throw new InvalidOperationException("Token'lar HTTP isteği dışında kaydedilemez.");
        context.Items[ItemsKey] = tokens;

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cevap başladığı için yenilenen token'lar cookie'ye yazılamadı.");
            return;
        }

        var existing = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = existing.Succeeded && existing.Properties is not null ? existing.Properties : new AuthenticationProperties();
        if (isPersistent.HasValue)
        {
            properties.IsPersistent = isPersistent.Value;
        }

        properties.AllowRefresh = true;
        properties.IssuedUtc = null;
        properties.ExpiresUtc = null;
        WriteTokens(properties, tokens);

        var principal = _principals.Create(tokens.AccessToken);
        context.User = principal;
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    /// <inheritdoc />
    public override async Task ClearAsync()
    {
        var context = _accessor.HttpContext;
        if (context is null)
        {
            return;
        }

        context.Items[ItemsKey] = null;
        if (!context.Response.HasStarted)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
