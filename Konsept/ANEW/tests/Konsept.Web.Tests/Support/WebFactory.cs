using AngleSharp;
using AngleSharp.Dom;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Konsept.Web.Tests.Support;

/// <summary>
/// Web'i süreç içinde açar; API istemcisi sahtedir (Web testleri API'ye gitmez). Default-deny (plan 0002): sahte API
/// varsayılan olarak girişi kabul eder ve oturumu geçerli sayar; oturumlu istemci gerçek <c>/giris</c> akışıyla açılır
/// (<see cref="CreateSignedInClientAsync"/>). İstenirse saat dışarıdan verilir (ör. 14 günlük oturum testleri).
/// </summary>
public sealed class WebFactory : WebApplicationFactory<Program>
{
    /// <summary>Testteki servis anahtarı (bilinen sır olarak kaydedilmeli; logda hiçbir biçimde geçmez).</summary>
    public const string ServiceKey = "web-test-service-key-0008";

    /// <summary>Sahte oturumun kullanıcı adı.</summary>
    public const string UserName = "Test Kullanıcı";

    /// <summary>Sahte oturumun kullanıcı kimliği.</summary>
    public static readonly Guid UserId = Guid.Parse("0195f3a0-0000-7000-8000-0000000000aa");

    private readonly TimeProvider? _time;

    /// <summary>Initializes a new instance of the <see cref="WebFactory"/> class (gerçek saat).</summary>
    public WebFactory()
        : this(null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WebFactory"/> class (xUnit fixture'ı tek public kurucu ister).</summary>
    /// <param name="time">Saat kaynağı; <c>null</c> ise gerçek saat.</param>
    internal WebFactory(TimeProvider? time)
    {
        _time = time;
        var clock = time ?? TimeProvider.System;
        Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<AuthSessionResponse>.Success(NewSession(clock, "jwt-login", "knr_login")));
        Api.RegisterAsync(Arg.Any<RegisterApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<AuthSessionResponse>.Success(NewSession(clock, "jwt-register", "knr_register")));
        Api.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<AuthSessionResponse>.Success(NewSession(clock, "jwt-refreshed", "knr_" + Guid.NewGuid().ToString("N"))));
        Api.GetSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<CurrentUserResponse>.Success(new CurrentUserResponse(UserId, UserName, SignedInClient.Email)));
        Api.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
    }

    /// <summary>Gets sahte API istemcisi.</summary>
    public IKonseptApiClient Api { get; } = Substitute.For<IKonseptApiClient>();

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "web-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Log klasöründeki tüm log metnini okur (dosya Serilog tarafından açıkken de).</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText() =>
        !Directory.Exists(LogDirectory)
            ? string.Empty
            : string.Concat(Directory.GetFiles(LogDirectory, "*.log").Select(f =>
            {
                using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }));

    /// <summary>Gerçek giriş akışıyla oturum açmış istemci (yönlendirme izlemez).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public Task<HttpClient> CreateSignedInClientAsync(CancellationToken cancellationToken) =>
        SignedInClient.CreateAsync(this, cancellationToken);

    /// <summary>Sayfayı getirir ve AngleSharp belgesine çevirir.</summary>
    /// <param name="client">HTTP istemcisi.</param>
    /// <param name="path">Sayfa yolu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>HTML belgesi.</returns>
    public static async Task<IDocument> GetDocumentAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        var html = await client.GetStringAsync(new Uri(path, UriKind.Relative), cancellationToken).ConfigureAwait(false);
        return await ParseAsync(html, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>HTML'i AngleSharp belgesine çevirir.</summary>
    /// <param name="html">HTML.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Belge.</returns>
    public static async Task<IDocument> ParseAsync(string html, CancellationToken cancellationToken)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(request => request.Content(html), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("Api:BaseUrl", "http://api.invalid/");
        builder.UseSetting("Logging:Directory", LogDirectory);
        builder.UseSetting("Internal:ServiceKey", ServiceKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKonseptApiClient>();
            services.AddSingleton(Api);
            if (_time is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(_time);
            }
        });
    }

    private static AuthSessionResponse NewSession(TimeProvider clock, string accessToken, string refreshToken) =>
        new(accessToken, clock.GetUtcNow().AddMinutes(15), refreshToken, UserId, UserName);
}
