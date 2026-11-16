using System.Net;
using Konsept.Web.Controllers;
using Konsept.Web.Tests.Support;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// "Ajanı indir" (spec 0009 AC-1): giriş yapmış kullanıcı Ayarlar'da ve "Ajan bağlı değil" durumunda bağlantıyı görür; bağlantı
/// ürün sürümünü taşıyan tek kurulum dosyasını indirir; giriş yapmamış kullanıcı indiremez. Bileşen aynası (Req 12) kimliksiz
/// statik yoldur ve yalnızca kendi klasörünü sunar.
/// </summary>
public sealed class AgentDownloadTests
{
    /// <summary>Giriş yapmış kullanıcı sürümlü kurulum dosyasını alır (HTTP 200, dosya adı ürün sürümüyle).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_SignedIn_ServesVersionedExe()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        Directory.CreateDirectory(factory.AgentDownloadDirectory);
        byte[] content = [0x4D, 0x5A, 1, 2, 3];
        await File.WriteAllBytesAsync(Path.Combine(factory.AgentDownloadDirectory, AgentDownloadController.CurrentFileName), content, ct);
        await File.WriteAllBytesAsync(Path.Combine(factory.AgentDownloadDirectory, "anewconcept-ajan-0.0.1-win-x64.exe"), [9], ct);
        using var client = await factory.CreateSignedInClientAsync(ct);

        using var response = await client.GetAsync(new Uri("/ajan/indir", UriKind.Relative), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var version = typeof(AgentDownloadController).Assembly.GetName().Version!;
        var expectedVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        AgentDownloadController.ProductVersion.ShouldBe(expectedVersion);
        response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe($"anewconcept-ajan-{expectedVersion}-win-x64.exe");
        (await response.Content.ReadAsByteArrayAsync(ct)).ShouldBe(content);
    }

    /// <summary>Giriş yapmamış kullanıcı indiremez; giriş sayfasına gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_Anonymous_RedirectsToLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        Directory.CreateDirectory(factory.AgentDownloadDirectory);
        await File.WriteAllBytesAsync(Path.Combine(factory.AgentDownloadDirectory, AgentDownloadController.CurrentFileName), [1], ct);
        using var client = SignedInClient.CreateAnonymous(factory);

        using var response = await client.GetAsync(new Uri("/ajan/indir", UriKind.Relative), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.ShouldBe("/giris");
    }

    /// <summary>Dosya sunucuda yoksa Türkçe 404.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_Missing_Returns404Turkish()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        using var response = await client.GetAsync(new Uri("/ajan/indir", UriKind.Relative), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(ct)).ShouldContain("Ajan kurulum dosyası şu an sunucuda yok");
    }

    /// <summary>Ayarlar'da "Ajanı indir" her zaman; durum alanında başlangıçta gizli (status.js bağlı değilken gösterir).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SettingsAndStatus_HaveDownloadLink()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var settings = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        var link = settings.GetElementById("settings-agent-download")!;
        link.TextContent.ShouldBe("Ajanı indir");
        link.GetAttribute("href").ShouldBe("/ajan/indir");
        link.HasAttribute("hidden").ShouldBeFalse();
        settings.GetElementById("settings-agent-title")!.TextContent.ShouldBe("Ajan");

        var home = await WebFactory.GetDocumentAsync(client, "/", ct);
        var status = home.GetElementById("status-agent-download")!;
        status.TextContent.ShouldBe("Ajanı indir");
        status.GetAttribute("href").ShouldBe("/ajan/indir");
        status.HasAttribute("hidden").ShouldBeTrue();
    }

    /// <summary>Bileşen aynası kimliksizdir, yalnızca kendi klasöründeki dosyaları verir; kurulum dosyası oradan alınamaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Mirror_Anonymous_ServesOnlyComponentFiles()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var mirror = Path.Combine(factory.AgentDownloadDirectory, "components");
        Directory.CreateDirectory(mirror);
        await File.WriteAllBytesAsync(Path.Combine(mirror, "ffmpeg-test.zip"), [7, 7], ct);
        await File.WriteAllBytesAsync(Path.Combine(factory.AgentDownloadDirectory, AgentDownloadController.CurrentFileName), [1], ct);
        using var client = SignedInClient.CreateAnonymous(factory);

        using var file = await client.GetAsync(new Uri("/ajan-bilesenleri/ffmpeg-test.zip", UriKind.Relative), ct);
        file.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await file.Content.ReadAsByteArrayAsync(ct)).ShouldBe([7, 7]);

        using var escape = await client.GetAsync(new Uri("/ajan-bilesenleri/..%2F" + AgentDownloadController.CurrentFileName, UriKind.Relative), ct);
        escape.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }
}
