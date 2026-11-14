using System.Net;
using Konsept.Shared.Kernel.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// İsteğin istemci adresi (hız sınırının bölümleme anahtarı, plan 0002 Risk 5): Web'in ilettiği
/// <see cref="ClientIpHeader.Name"/> başlığına yalnızca geçerli servis anahtarıyla birlikte güvenilir; aksi halde
/// bağlantının uzak adresi kullanılır.
/// </summary>
public static class ClientAddress
{
    /// <summary>Uzak adres bilinmiyorsa kullanılan anahtar.</summary>
    public const string Unknown = "unknown";

    /// <summary>İstemci adresini çözer.</summary>
    /// <param name="context">HTTP bağlamı.</param>
    /// <returns>IP adresi metni ya da <see cref="Unknown"/>.</returns>
    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var forwarded = context.Request.Headers[ClientIpHeader.Name].ToString();
        if (!string.IsNullOrEmpty(forwarded)
            && IPAddress.TryParse(forwarded, out var address)
            && ServiceKeyAuthenticationHandler.HasValidServiceKey(context.RequestServices.GetRequiredService<IConfiguration>(), context.Request))
        {
            return address.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? Unknown;
    }
}
