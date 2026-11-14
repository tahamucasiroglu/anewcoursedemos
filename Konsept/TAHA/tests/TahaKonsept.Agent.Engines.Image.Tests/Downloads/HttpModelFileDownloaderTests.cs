using System.Net;
using System.Security.Cryptography;
using Shouldly;
using TahaKonsept.Agent.Engines.Image.Downloads;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.Downloads;

/// <summary>Devam eden indirici testleri (Range, kopma, SHA-256, atlama).</summary>
public sealed class HttpModelFileDownloaderTests : IDisposable
{
    private static readonly Uri Source = new("https://huggingface.co/repo/resolve/main/model.gguf");
    private readonly TempDirectory _temp = new();
    private readonly byte[] _content = RandomNumberGenerator.GetBytes(3 * 1024 * 1024 + 123);

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Bağlantı kopunca ikinci denemede Range ile kaldığı yerden devam eder ve hash doğrular.</summary>
    [Fact]
    public async Task Resumes_after_connection_drop_with_range_request()
    {
        var responder = new RangeContentResponder(_content) { BreakFirstAfter = 1_000_000 };
        var handler = new StubHttpMessageHandler(responder.Respond);
        var target = Path.Combine(_temp.Path, "sub", "model.gguf");
        var reports = new List<ModelDownloadProgress>();

        var result = await EngineFactory.CreateDownloader(handler)
            .DownloadAsync(Source, target, _content.Length, Sha(_content), new SyncProgress<ModelDownloadProgress>(reports), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        (await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken)).ShouldBe(_content);
        File.Exists(target + ".part").ShouldBeFalse();
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].RangeFrom.ShouldBeNull();
        handler.Requests[1].RangeFrom.ShouldBe(1_000_000);
        reports.Last().BytesReceived.ShouldBe(_content.Length);
    }

    /// <summary>Var olan .part dosyasından devam eder; sunucu Range'i yok sayarsa baştan yazar.</summary>
    [Fact]
    public async Task Restarts_when_server_ignores_range()
    {
        var target = Path.Combine(_temp.Path, "model.gguf");
        await File.WriteAllBytesAsync(target + ".part", new byte[500], TestContext.Current.CancellationToken);
        var handler = new StubHttpMessageHandler(new RangeContentResponder(_content) { IgnoreRange = true }.Respond);

        var result = await EngineFactory.CreateDownloader(handler)
            .DownloadAsync(Source, target, _content.Length, Sha(_content), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        handler.Requests.Single().RangeFrom.ShouldBe(500);
        (await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken)).ShouldBe(_content);
    }

    /// <summary>Hedef doğru boyutta varsa ağa gitmez.</summary>
    [Fact]
    public async Task Skips_existing_complete_file()
    {
        var target = Path.Combine(_temp.Path, "model.gguf");
        await File.WriteAllBytesAsync(target, _content, TestContext.Current.CancellationToken);
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Ağa gidilmemeli"));

        var result = await EngineFactory.CreateDownloader(handler)
            .DownloadAsync(Source, target, _content.Length, null, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        handler.Requests.ShouldBeEmpty();
    }

    /// <summary>SHA-256 uyuşmazsa .part silinir ve hata döner.</summary>
    [Fact]
    public async Task Fails_and_deletes_part_on_checksum_mismatch()
    {
        var target = Path.Combine(_temp.Path, "model.gguf");
        var handler = new StubHttpMessageHandler(new RangeContentResponder(_content).Respond);

        var result = await EngineFactory.CreateDownloader(handler)
            .DownloadAsync(Source, target, _content.Length, new string('0', 64), null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("download.checksum");
        File.Exists(target).ShouldBeFalse();
        File.Exists(target + ".part").ShouldBeFalse();
    }

    /// <summary>401/403: kapılı depo mesajıyla hata (yeniden denemeden).</summary>
    [Fact]
    public async Task Reports_gated_repository_on_unauthorized()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await EngineFactory.CreateDownloader(handler)
            .DownloadAsync(Source, Path.Combine(_temp.Path, "m.gguf"), 10, null, null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Hugging Face");
        handler.Requests.Count.ShouldBe(1);
    }

    private static string Sha(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));
}
