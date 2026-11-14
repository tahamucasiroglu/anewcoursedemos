using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TahaKonsept.Web.Infrastructure.Auth.Base;

namespace TahaKonsept.Web.Infrastructure.Auth.Concrete;

/// <summary>
/// JWT'nin payload bölümünü (base64url JSON) çözerek claim'leri okur. Dizi değerli claim'ler (ör. permission listesi)
/// her eleman için ayrı claim olur. Token bozuksa boş liste döner ve uyarı günlüğe yazılır.
/// </summary>
public sealed class JwtPrincipalFactory : PrincipalFactoryBase
{
    private readonly ILogger<JwtPrincipalFactory> _logger;

    /// <summary>Günlükleyiciyi alır.</summary>
    /// <param name="logger">Günlükleyici.</param>
    public JwtPrincipalFactory(ILogger<JwtPrincipalFactory> logger) => _logger = logger;

    /// <inheritdoc />
    protected override IEnumerable<Claim> ReadClaims(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            _logger.LogWarning("Erişim token'ı JWT biçiminde değil.");
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            var claims = new List<Claim>();
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(property.Value.EnumerateArray().Select(v => new Claim(property.Name, ValueOf(v))));
                }
                else
                {
                    claims.Add(new Claim(property.Name, ValueOf(property.Value)));
                }
            }

            return claims;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            _logger.LogWarning(ex, "Erişim token'ı çözülemedi.");
            return [];
        }
    }

    private static string ValueOf(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.GetRawText();

    private static byte[] DecodeBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + ((4 - (s.Length % 4)) % 4), '=');
        return Convert.FromBase64String(s);
    }
}
