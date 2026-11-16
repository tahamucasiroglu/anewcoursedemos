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
