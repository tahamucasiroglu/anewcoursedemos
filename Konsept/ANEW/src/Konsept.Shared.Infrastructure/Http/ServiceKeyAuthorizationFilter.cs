using System.Security.Cryptography;
using System.Text;
using Konsept.Shared.Kernel.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// İç uçları (ör. Web'in log iletimi) paylaşılan servis anahtarıyla korur (plan 0008, Risk 2). Anahtar
/// yapılandırılmamışsa her istek reddedilir (kapalı varsayılan); karşılaştırma sabit zamanlıdır. Çerçeve adaptörü
/// (MVC filter).
/// </summary>
/// <param name="configuration">Yapılandırma (<see cref="ServiceKeyHeader.ConfigurationKey"/>).</param>
public sealed class ServiceKeyAuthorizationFilter(IConfiguration configuration) : IAuthorizationFilter
{
    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var expected = configuration[ServiceKeyHeader.ConfigurationKey];
        var given = context.HttpContext.Request.Headers[ServiceKeyHeader.Name].ToString();
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(given)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given)))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Servis anahtarı geçersiz.",
            });
        }
    }
}
