using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Downloads.Abstract;
using TahaKonsept.Web.Services.Onboarding.Abstract;

namespace TahaKonsept.Web.Tests.Support;

/// <summary>
/// Web host'unu bellek içinde ayağa kaldıran fabrika (API gerekmez): kimlik doğrulama <see cref="TestAuthHandler"/>,
/// <see cref="Releases"/> ve <see cref="Onboarding"/> sahte servislerdir (testler cevaplarını ayarlar). Data Protection
/// anahtarları geçici klasöre yazılır.
/// </summary>
public sealed class WebFactory : WebApplicationFactory<TahaKonsept.Web.Program>
{
    private readonly TempDirectory _keys = new();

    /// <summary>Sahte kurulum dosyası bilgisi sağlayıcısı.</summary>
    public IAgentReleaseProvider Releases { get; } = Substitute.For<IAgentReleaseProvider>();

    /// <summary>Sahte kontrol listesi servisi.</summary>
    public IOnboardingService Onboarding { get; } = Substitute.For<IOnboardingService>();

    /// <summary>Kimliği doğrulanmış istemci (yönlendirmeleri izlemez).</summary>
    /// <param name="user">Kullanıcı adı.</param>
    public HttpClient CreateUserClient(string user = "taha")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("DataProtection:KeysDirectory", _keys.Path);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o => o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
            services.AddSingleton(Releases);
            services.AddSingleton(Onboarding);
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keys.Dispose();
        }
    }

    /// <summary>Örnek "yayınlanmış" kurulum bilgisi.</summary>
    /// <param name="version">Sürüm.</param>
    public static AgentReleaseInfo Published(string version) => new()
    {
        IsAvailable = true,
        FileName = "TahaKonsept.Agent-win-Setup.exe",
        DownloadUrl = "/downloads/TahaKonsept.Agent-win-Setup.exe?v=" + version,
        Version = version,
        SizeBytes = 91104066,
        PublishedAtUtc = new DateTimeOffset(2026, 11, 14, 16, 56, 0, TimeSpan.Zero),
    };
}
