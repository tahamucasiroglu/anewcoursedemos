using System.Net;
using System.Net.Http.Headers;
using Konsept.Agent.Infrastructure.Base;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.Extensions.Time.Testing;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Yerel video akışı (spec 0017 Req 21, AC-25, AC-28; H-1): gerçek loopback sunucusu, sahte saat. Yetkisiz, süresi dolmuş, başka videonun ve
/// başka ajanın (başka kullanıcının) yetkisi reddedilir; yalnızca 127.0.0.1 dinlenir; açılmış akış yetki süresinden uzun sürse de kesilmez;
/// yetki hiçbir loga yazılmaz.
/// </summary>
public sealed class LocalVideoServerTests : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "anewconcept-stream-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly List<LogEvent> _logs = [];
    private readonly byte[] _video = [.. Enumerable.Range(0, 300_000).Select(i => (byte)(i % 251))];
    private Serilog.Core.Logger _logger = null!;
    private LocalVideoServer _server = null!;
    private HttpClient _http = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        await File.WriteAllBytesAsync(Path.Combine(_folder, "video.mp4"), _video);
        _logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(new DelegateSink(e =>
        {
            lock (_logs)
            {
                _logs.Add(e);
            }
        })).CreateLogger();
        _server = new LocalVideoServer(() => _folder, _time, _logger, preferredPort: 0);
        _server.Start();
        _http = new HttpClient();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _server.DisposeAsync();
        await _logger.DisposeAsync();
        Directory.Delete(_folder, recursive: true);
    }

    /// <summary>AC-25: yalnızca loopback; yetkiyle tam dosya ve Range (206); dosya yoksa ya da yol verilirse "bulunamadı".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stream_LoopbackOnly_ServesWithRange()
    {
        var ct = TestContext.Current.CancellationToken;
        _server.Address.ShouldBe(IPAddress.Loopback);
        var url = Open(Guid.NewGuid());
        url.ShouldStartWith("http://127.0.0.1:" + _server.Port + "/video/");

        (await _http.GetByteArrayAsync(new Uri(url), ct)).ShouldBe(_video);
        using var ranged = new HttpRequestMessage(HttpMethod.Get, url);
        ranged.Headers.Range = new RangeHeaderValue(10, 19);
        using var partial = await _http.SendAsync(ranged, ct);
        partial.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        partial.Content.Headers.ContentRange!.ToString().ShouldBe("bytes 10-19/300000");
        (await partial.Content.ReadAsByteArrayAsync(ct)).ShouldBe(_video[10..20]);

        _server.Open(new AgentStreamRequest(Guid.NewGuid(), "yok.mp4")).Error.ShouldBe(AgentStreamReply.NotFound);
        _server.Open(new AgentStreamRequest(Guid.NewGuid(), "..\\video.mp4")).Error.ShouldBe(AgentStreamReply.NotFound);
        _server.Open(new AgentStreamRequest(Guid.NewGuid(), Path.Combine(_folder, "video.mp4"))).Error.ShouldBe(AgentStreamReply.NotFound);
    }

    /// <summary>
    /// AC-25 (kasıtlı bozma: yetki denetimi kaldırılınca kırmızı): yetkisiz, yanlış, süresi dolmuş, başka videonun ve başka ajanın yetkisi 403.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stream_Rejects_Unauthorized_Expired_OtherVideo_OtherAgent()
    {
        var ct = TestContext.Current.CancellationToken;
        var video = Guid.NewGuid();
        var url = Open(video);
        var bare = url[..url.IndexOf('?', StringComparison.Ordinal)];

        (await StatusAsync(bare, ct)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(bare + "?t=yanlis", ct)).ShouldBe(HttpStatusCode.Forbidden);
        var other = Guid.NewGuid();
        Open(other);
        (await StatusAsync(url.Replace(video.ToString("N"), other.ToString("N"), StringComparison.Ordinal), ct)).ShouldBe(HttpStatusCode.Forbidden);

        await using (var otherAgent = new LocalVideoServer(() => _folder, _time, _logger, preferredPort: 0))
        {
            otherAgent.Start();
            var foreign = otherAgent.Open(new AgentStreamRequest(video, "video.mp4")).Url!;
            (await StatusAsync(url[..url.IndexOf('?', StringComparison.Ordinal)] + foreign[foreign.IndexOf('?', StringComparison.Ordinal)..], ct))
                .ShouldBe(HttpStatusCode.Forbidden);
        }

        _time.Advance(VideoStreamHostBase.OpenWindow + TimeSpan.FromSeconds(1));
        (await StatusAsync(url, ct)).ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>AC-25: ilk açılıştan sonra oynatma sürdükçe yetki süresi dolsa da akış kesilmez (uzun video sonuna kadar).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Stream_LongerThanToken_KeepsPlaying()
    {
        var ct = TestContext.Current.CancellationToken;
        var url = Open(Guid.NewGuid());
        (await StatusAsync(url, ct)).ShouldBe(HttpStatusCode.OK);
        for (var minute = 0; minute < 20; minute++)
        {
            _time.Advance(TimeSpan.FromMinutes(1)); // 20 dakikalık video: yetki 60 sn, oynatıcı aralıklarla parça ister.
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(minute * 1000, (minute * 1000) + 999);
            using var response = await _http.SendAsync(request, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        }
    }

    /// <summary>AC-28 (kasıtlı bozma: istek yolu loglanınca kırmızı): açılış, oynatma ve ret sırasında hiçbir log yetkiyi düz metin içermez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Token_NeverLogged()
    {
        var ct = TestContext.Current.CancellationToken;
        var url = Open(Guid.NewGuid());
        var token = url[(url.IndexOf("?t=", StringComparison.Ordinal) + 3)..];
        (await StatusAsync(url, ct)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(url.Replace(token, token[..10], StringComparison.Ordinal), ct)).ShouldBe(HttpStatusCode.Forbidden);
        _time.Advance(TimeSpan.FromHours(1));
        (await StatusAsync(url, ct)).ShouldBe(HttpStatusCode.Forbidden);
        await Task.Delay(200, ct);

        List<string> rendered;
        lock (_logs)
        {
            rendered = [.. _logs.Select(e => e.RenderMessage() + " " + string.Join(' ', e.Properties.Values) + " " + e.Exception)];
        }

        var prefix = token.Substring(0, 10);
        rendered.ShouldNotBeEmpty();
        rendered.ShouldAllBe(l => !l.Contains(prefix, StringComparison.Ordinal) && !l.Contains("?t=", StringComparison.Ordinal));
    }

    private string Open(Guid video) => _server.Open(new AgentStreamRequest(video, "video.mp4")).Url!;

    private async Task<HttpStatusCode> StatusAsync(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(new Uri(url), ct);
        return response.StatusCode;
    }
}
