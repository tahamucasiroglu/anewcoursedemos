using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TahaKonsept.IntegrationTests.Support;

/// <summary>
/// API host'unu bellek içi test sunucusunda ayağa kaldıran fabrika (<c>Testing</c> ortamı; .env yüklenmez).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Test ortamını ayarlar.</summary>
    /// <param name="builder">Web host oluşturucu.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
