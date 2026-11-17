using System.Net;
using Konsept.Agent.Infrastructure.Setup;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Gerçek indirici (spec 0009 AC-11, AC-12; review r1 M-1): bağlantı sessizce koparsa (veri gelmiyor, hata da yok) indirme sonsuza dek
/// beklemez; hareketsizlik süresi dolunca <see cref="IOException"/> atar — motor bunu yeniden deneme / ayna / "İnternet bağlantısı koptu"
/// yoluna sokar. Ağ yok: sahte handler.
/// </summary>
public sealed class WindowsSetupHostTests
{
    /// <summary>Gövde gelmeyi bırakınca indirme hareketsizlik süresinden sonra IOException ile biter; yarım dosya kalabilir (motor siler).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_StalledStream_FailsAfterInactivityTimeout()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-host-");
        try
        {
            using var http = new HttpClient(new StallingHandler());
            var host = new WindowsSetupHost(http, TimeSpan.FromMilliseconds(300));
            var started = DateTime.UtcNow;

            await Should.ThrowAsync<IOException>(() =>
                host.DownloadAsync(new Uri("https://example.invalid/a.zip"), Path.Combine(dir.FullName, "a.zip"), null, TestContext.Current.CancellationToken));

            (DateTime.UtcNow - started).ShouldBeLessThan(TimeSpan.FromSeconds(10));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Kullanıcı iptali (tray "Çıkış") IOException değil, iptal olarak kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_UserCancel_StaysCancellation()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-host-");
        try
        {
            using var http = new HttpClient(new StallingHandler());
            var host = new WindowsSetupHost(http, TimeSpan.FromSeconds(30));
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            await Should.ThrowAsync<OperationCanceledException>(() =>
                host.DownloadAsync(new Uri("https://example.invalid/a.zip"), Path.Combine(dir.FullName, "a.zip"), null, cts.Token));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// npm kurulumu kullanıcının kendi npm önbelleğini (<c>%LOCALAPPDATA%\npm-cache</c>) kullanmaz: önbellek ve günlükler bileşen klasörünün
    /// içindedir (spec 0009 Req 6–7; gerçek kurulum testinde kullanıcı önbelleğine günlük yazıldığı ve eski günlüklerin silindiği görüldü).
    /// </summary>
    [Fact]
    public void NpmStartInfo_CacheInsideDestination_NotUserCache()
    {
        var destination = Path.Combine(Path.GetTempPath(), "anewconcept-npm-dest");
        var node = Path.Combine(Path.GetTempPath(), "anewconcept-npm-node");

        var info = WindowsSetupHost.CreateNpmStartInfo(node, destination);

        info.FileName.ShouldBe(Path.Combine(node, "node.exe"));
        info.WorkingDirectory.ShouldBe(destination);
        info.ArgumentList.ShouldContain("ci");
        info.ArgumentList.ShouldContain("--ignore-scripts");
        info.Environment["npm_config_cache"].ShouldBe(Path.Combine(destination, ".npm-cache"));
        info.Environment["npm_config_cache"]!.ShouldStartWith(destination);
        info.Environment["npm_config_update_notifier"].ShouldBe("false");
        info.Environment["HYPERFRAMES_NO_TELEMETRY"].ShouldBe("1");
    }

    /// <summary>Kurulum hangi yoldan biterse bitsin (burada Node yok → başarısız) bileşenin npm önbelleği silinir; kurulum yerinde iz kalmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task InstallPackages_AnyOutcome_RemovesOwnNpmCache()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-host-");
        try
        {
            var destination = Directory.CreateDirectory(Path.Combine(dir.FullName, "hyperframes.partial")).FullName;
            var cache = Directory.CreateDirectory(WindowsSetupHost.NpmCacheDirectory(destination)).FullName;
            await File.WriteAllTextAsync(Path.Combine(cache, "index"), "x", TestContext.Current.CancellationToken);
            var hf = ComponentCatalog.Load().Single(c => c.Id == "hyperframes");
            using var http = new HttpClient(new StallingHandler());
            var host = new WindowsSetupHost(http);

            var ok = await host.InstallPackagesAsync("hyperframes", hf.Sha256, Path.Combine(dir.FullName, "no-node"), destination, TestContext.Current.CancellationToken);

            ok.ShouldBeFalse();
            Directory.Exists(cache).ShouldBeFalse();
            File.Exists(Path.Combine(destination, "package-lock.json")).ShouldBeTrue();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Başlıkları hemen, gövdeden bir parça verip sonra susan sunucu.</summary>
    private sealed class StallingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) });
    }

    /// <summary>İlk okumada birkaç bayt, sonra iptal edilene dek bekleyen akış.</summary>
    private sealed class StallingStream : Stream
    {
        private bool _sent;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_sent)
            {
                _sent = true;
                buffer.Span[0] = 1;
                return 1;
            }

            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
