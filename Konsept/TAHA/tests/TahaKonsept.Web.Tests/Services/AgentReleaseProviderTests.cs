using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Downloads.Base;
using TahaKonsept.Web.Services.Downloads.Concrete;
using TahaKonsept.Web.Tests.Support;
using Xunit;

namespace TahaKonsept.Web.Tests.Services;

/// <summary>
/// <see cref="WebRootAgentReleaseProvider"/> testleri: sürüm Velopack akışından (en yüksek Full paket), boyut dosyadan;
/// dosya yoksa "yok" bilgisi, akış bozuk/eksikse sürüm null ama indirme açık.
/// </summary>
public sealed class AgentReleaseProviderTests : IDisposable
{
    private readonly TempDirectory _root = new();
    private readonly string _downloads;

    /// <summary>wwwroot/downloads klasörünü hazırlar.</summary>
    public AgentReleaseProviderTests()
    {
        _downloads = Path.Combine(_root.Path, "downloads");
        Directory.CreateDirectory(_downloads);
    }

    /// <summary>En yüksek Full sürüm ve gerçek dosya boyutu okunur; adres sürümle önbellek kırar.</summary>
    [Fact]
    public void Reads_latest_full_version_and_file_size()
    {
        WriteSetup(1536);
        WriteFeed("""
            {"Assets":[
              {"PackageId":"TahaKonsept.Agent","Version":"0.2.3","Type":"Full","FileName":"a.nupkg","Size":1},
              {"PackageId":"TahaKonsept.Agent","Version":"0.2.10","Type":"Full","FileName":"b.nupkg","Size":1},
              {"PackageId":"TahaKonsept.Agent","Version":"0.3.0","Type":"Delta","FileName":"c.nupkg","Size":1},
              {"PackageId":"TahaKonsept.Agent","Version":"0.2.4","Type":"Full","FileName":"d.nupkg","Size":1}
            ]}
            """);

        var info = CreateProvider().GetCurrent();

        info.IsAvailable.ShouldBeTrue();
        info.Version.ShouldBe("0.2.10");
        info.SizeBytes.ShouldBe(1536);
        info.SizeText.ShouldBe("2 KB");
        info.FileName.ShouldBe(AgentReleaseProviderBase.SetupFileName);
        info.DownloadUrl.ShouldBe("/downloads/TahaKonsept.Agent-win-Setup.exe?v=0.2.10");
        info.PublishedAtUtc.ShouldNotBeNull();
    }

    /// <summary>Kurulum dosyası yoksa indirme kapalıdır (bozuk bağlantı yerine uyarı gösterilir).</summary>
    [Fact]
    public void Missing_setup_file_is_reported_as_unavailable()
    {
        WriteFeed("""{"Assets":[{"Version":"0.2.4","Type":"Full"}]}""");

        var info = CreateProvider().GetCurrent();

        info.IsAvailable.ShouldBeFalse();
        info.Version.ShouldBeNull();
        info.SizeBytes.ShouldBe(0);
        info.FileName.ShouldBe(AgentReleaseProviderBase.SetupFileName);
    }

    /// <summary>Bozuk akışta sürüm null olur, dosya yine indirilebilir.</summary>
    [Fact]
    public void Corrupt_feed_gives_null_version_but_keeps_download()
    {
        WriteSetup(10);
        WriteFeed("{ bu json değil");

        var info = CreateProvider().GetCurrent();

        info.IsAvailable.ShouldBeTrue();
        info.Version.ShouldBeNull();
        info.DownloadUrl.ShouldBe("/downloads/TahaKonsept.Agent-win-Setup.exe");
    }

    /// <summary>Akış dosyası yoksa sürüm null olur.</summary>
    [Fact]
    public void Missing_feed_gives_null_version()
    {
        WriteSetup(10);

        CreateProvider().GetCurrent().Version.ShouldBeNull();
    }

    /// <summary>Full paket yoksa Delta sürümlerinden en yükseği alınır; ön sürüm eki karşılaştırmayı bozmaz.</summary>
    [Fact]
    public void Falls_back_to_any_package_type_and_ignores_prerelease_suffix()
    {
        WriteSetup(10);
        WriteFeed("""{"assets":[{"version":"1.0.0-beta","type":"Delta"},{"version":"0.9.9","type":"Delta"}]}""");

        CreateProvider().GetCurrent().Version.ShouldBe("1.0.0-beta");
    }

    /// <summary>Boyut metni tr-TR biçimindedir.</summary>
    [Fact]
    public void Formats_size_in_turkish()
    {
        AgentReleaseInfo.FormatSize(91104066).ShouldBe("86,9 MB");
        AgentReleaseInfo.FormatSize(512).ShouldBe("512 B");
        AgentReleaseInfo.FormatSize(3L << 30).ShouldBe("3,0 GB");
    }

    /// <inheritdoc />
    public void Dispose() => _root.Dispose();

    private WebRootAgentReleaseProvider CreateProvider()
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(_root.Path);
        environment.ContentRootPath.Returns(_root.Path);
        return new WebRootAgentReleaseProvider(environment, NullLogger<WebRootAgentReleaseProvider>.Instance);
    }

    private void WriteSetup(int size) => File.WriteAllBytes(Path.Combine(_downloads, AgentReleaseProviderBase.SetupFileName), new byte[size]);

    private void WriteFeed(string json) => File.WriteAllText(Path.Combine(_downloads, AgentReleaseProviderBase.FeedFileName), json);
}
