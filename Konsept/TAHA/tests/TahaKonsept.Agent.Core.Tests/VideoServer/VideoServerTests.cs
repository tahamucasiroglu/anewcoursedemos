using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Shouldly;
using TahaKonsept.Agent.Core.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.VideoServer;

/// <summary>Kestrel video sunucusu uçtan uca: anahtar denetimi, 200/206/416, HEAD, 404.</summary>
public sealed class VideoServerTests
{
    private const string Key = "test-anahtar";

    /// <summary>Range istekleri doğru parçayı ve başlıkları döner; yanlış anahtar 403.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Server_ServesRanges_AndChecksKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();
        var videoId = Guid.NewGuid();
        var file = Path.Combine(temp.Path, "video.mp4");
        var content = Enumerable.Range(0, 1000).Select(i => (byte)(i % 251)).ToArray();
        await File.WriteAllBytesAsync(file, content, ct);
        var port = FreePort();
        await using var server = new TestVideoServer(port, Key, videoId, file);

        var started = await server.StartAsync(ct);
        started.IsSuccess.ShouldBeTrue(started.Error.Message);
        server.IsRunning.ShouldBeTrue();
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + port) };
        var url = new Uri("/videos/" + videoId + "?k=" + Key, UriKind.Relative);

        using (var full = await client.GetAsync(url, ct))
        {
            full.StatusCode.ShouldBe(HttpStatusCode.OK);
            full.Headers.AcceptRanges.ShouldContain("bytes");
            (await full.Content.ReadAsByteArrayAsync(ct)).ShouldBe(content);
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Range = new RangeHeaderValue(10, 19);
            using var partial = await client.SendAsync(request, ct);
            partial.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
            partial.Content.Headers.ContentRange!.ToString().ShouldBe("bytes 10-19/1000");
            (await partial.Content.ReadAsByteArrayAsync(ct)).ShouldBe(content[10..20]);
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.TryAddWithoutValidation("Range", "bytes=5000-");
            using var unsatisfiable = await client.SendAsync(request, ct);
            unsatisfiable.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        using (var request = new HttpRequestMessage(HttpMethod.Head, url))
        {
            using var head = await client.SendAsync(request, ct);
            head.StatusCode.ShouldBe(HttpStatusCode.OK);
            head.Content.Headers.ContentLength.ShouldBe(1000);
        }

        using (var wrongKey = await client.GetAsync(new Uri("/videos/" + videoId + "?k=yanlis", UriKind.Relative), ct))
        {
            wrongKey.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        using (var missing = await client.GetAsync(new Uri("/videos/" + Guid.NewGuid() + "?k=" + Key, UriKind.Relative), ct))
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        await server.StopAsync(ct);
        server.IsRunning.ShouldBeFalse();
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
