using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Yalnızca testte: uygulamanın tüm hattı kurulduktan sonra "/__test/throw" isteğinde kasıtlı bir beklenmeyen
/// hata fırlatan, "/__test/slow" isteğinde ise istemci iptal edene kadar bekleyen son halka ekler; böylece hata
/// yakalama ve loglama gerçek hat üzerinden sınanır.
/// </summary>
public sealed class ThrowingEndpointStartupFilter : IStartupFilter
{
    public const string Path = "/__test/throw";
    public const string SlowPath = "/__test/slow";
    public const string SecretMessage = "gizli-ic-ayrinti-42";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.Run(async context =>
        {
            if (context.Request.Path == Path)
            {
                throw new InvalidOperationException(SecretMessage);
            }

            if (context.Request.Path == SlowPath)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
            }
        });
    };
}
