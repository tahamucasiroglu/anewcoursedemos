using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Yalnızca testte: "/__test/throw" isteğinde kasıtlı beklenmeyen hata fırlatan, "/__test/slow" isteğinde istemci iptal
/// edene kadar bekleyen iki uç ekler; hata yakalama ve loglama gerçek hat üzerinden sınanır. Uçlar açıkça anonimdir —
/// API'nin "varsayılan reddet" kuralı (spec 0004) bu yardımcı uçlara takılmasın diye.
/// </summary>
public sealed class ThrowingEndpointStartupFilter : IStartupFilter
{
    public const string Path = "/__test/throw";
    public const string SlowPath = "/__test/slow";
    public const string SecretMessage = "gizli-ic-ayrinti-42";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet(Path, (Func<HttpContext, Task>)(_ => throw new InvalidOperationException(SecretMessage))).AllowAnonymous();
            endpoints.MapGet(SlowPath, (Func<HttpContext, Task>)(context => Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted))).AllowAnonymous();
        });
    };
}
