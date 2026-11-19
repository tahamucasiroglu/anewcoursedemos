using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Yalnızca testte: test sunucusu isteklere uzak adres vermez; her isteğe Pi'deki gibi yerel vekil adresi (127.0.0.1)
/// verilir. Böylece nginx'in X-Forwarded-For başlığı gerçek hat üzerinden işlenir ve audit/hız sınırı IP'yi görür.
/// </summary>
public sealed class LoopbackRemoteAddressStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
            return nextMiddleware(context);
        });
        next(app);
    };
}
