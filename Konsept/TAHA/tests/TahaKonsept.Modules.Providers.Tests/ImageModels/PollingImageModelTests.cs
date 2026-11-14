using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.ImageModels;

/// <summary>
/// Gönder + sorgula görsel modelleri: BFL (x-key, polling_url, Ready → result.sample, moderasyon → refused) ve Replicate
/// (Bearer, predictions, urls.get, succeeded → output, failed → bad_request).
/// </summary>
public sealed class PollingImageModelTests
{
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>BFL: gönder → bekliyor → hazır → indir.</summary>
    [Fact]
    public async Task Bfl_submit_poll_download()
    {
        _http.Json("""{"id":"job-1","polling_url":"https://api.eu.bfl.ai/v1/get_result?id=job-1"}""")
            .Json("""{"id":"job-1","status":"Pending"}""")
            .Json("""{"id":"job-1","status":"Ready","result":{"sample":"https://delivery.bfl.ai/x.png"}}""")
            .Bytes(MediaTestData.Png, "image/png");

        var result = await Bfl().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.Images.Single().ShouldBe(MediaTestData.Png);
        result.Value.CostUsd.ShouldBe(0.009m);
        _http.Requests.Count.ShouldBe(4);
        var submit = _http.Requests[0];
        submit.Uri.ToString().ShouldBe("https://api.bfl.ai/v1/flux-3-image-klein");
        submit.Headers["x-key"].ShouldBe("sk-test-1234567890");
        submit.Json["width"]!.GetValue<int>().ShouldBe(1088);
        submit.Json["height"]!.GetValue<int>().ShouldBe(1920);
        submit.Json["seed"]!.GetValue<long>().ShouldBe(42);
        _http.Requests[1].Uri.ToString().ShouldBe("https://api.eu.bfl.ai/v1/get_result?id=job-1");
        _http.Requests[1].Headers["x-key"].ShouldBe("sk-test-1234567890");
        _http.Requests[3].Headers.ContainsKey("x-key").ShouldBeFalse();
    }

    /// <summary>BFL: içerik denetimi → providers.refused.</summary>
    [Fact]
    public async Task Bfl_moderation_is_refused()
    {
        _http.Json("""{"id":"job-2","polling_url":"https://api.bfl.ai/v1/get_result?id=job-2"}""")
            .Json("""{"id":"job-2","status":"Content Moderated"}""");

        var result = await Bfl().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(ProviderErrorCodes.Refused);
    }

    /// <summary>BFL: durum sorgusunda ağ kopması yalnız sorguyu yeniden dener; iş yeniden gönderilmez (ikinci kez ücretlenmez).</summary>
    [Fact]
    public async Task Bfl_poll_network_blip_retries_poll_only()
    {
        _http.Json("""{"id":"job-3","polling_url":"https://api.bfl.ai/v1/get_result?id=job-3"}""")
            .Throw(new HttpRequestException("bağlantı sıfırlandı"))
            .Json("""{"id":"job-3","status":"Ready","result":{"sample":"https://delivery.bfl.ai/y.png"}}""")
            .Bytes(MediaTestData.Png, "image/png");

        var result = await Bfl().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        _http.Requests.Count(r => r.Uri.ToString() == "https://api.bfl.ai/v1/flux-3-image-klein").ShouldBe(1);
    }

    /// <summary>BFL: sonuç indirilemezse iş yeniden gönderilmez; hata job_result_lost ve gönderilen görsel ücretiyle kayda geçer.</summary>
    [Fact]
    public async Task Bfl_lost_result_is_not_resubmitted_and_is_billed()
    {
        _http.Json("""{"id":"job-4","polling_url":"https://api.bfl.ai/v1/get_result?id=job-4"}""")
            .Json("""{"id":"job-4","status":"Ready","result":{"sample":"https://delivery.bfl.ai/z.png"}}""")
            .Json("""{"error":"boom"}""", System.Net.HttpStatusCode.BadGateway)
            .Json("""{"error":"boom"}""", System.Net.HttpStatusCode.BadGateway)
            .Json("""{"error":"boom"}""", System.Net.HttpStatusCode.BadGateway);

        var result = await Bfl().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(ProviderErrorCodes.JobResultLost);
        result.Error.Details.ShouldBeNull();
        _http.Requests.Count(r => r.Uri.ToString() == "https://api.bfl.ai/v1/flux-3-image-klein").ShouldBe(1);
        _recorder.Records.Single().Usage.Images.ShouldBe(1);
    }

    /// <summary>Replicate: predictions → urls.get sorgusu → output indirilir.</summary>
    [Fact]
    public async Task Replicate_prediction()
    {
        _http.Json("""{"id":"p1","status":"starting","urls":{"get":"https://api.replicate.com/v1/predictions/p1"}}""")
            .Json("""{"id":"p1","status":"processing"}""")
            .Json("""{"id":"p1","status":"succeeded","output":["https://replicate.delivery/a.png"]}""")
            .Bytes(MediaTestData.Png, "image/png");

        var result = await Replicate().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.CostUsd.ShouldBe(0.003m);
        var submit = _http.Requests[0];
        submit.Uri.ToString().ShouldBe("https://api.replicate.com/v1/models/black-forest-labs/flux-schnell/predictions");
        submit.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        submit.Json["input"]!["aspect_ratio"]!.GetValue<string>().ShouldBe("9:16");
        submit.Json["input"]!["num_outputs"]!.GetValue<int>().ShouldBe(1);
        _http.Requests[1].Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
    }

    /// <summary>Replicate: failed → providers.bad_request, hata mesajı taşınır.</summary>
    [Fact]
    public async Task Replicate_failure()
    {
        _http.Json("""{"id":"p2","status":"starting","urls":{"get":"https://api.replicate.com/v1/predictions/p2"}}""")
            .Json("""{"id":"p2","status":"failed","error":"NSFW content detected"}""");

        var result = await Replicate().GenerateAsync(MediaTestData.Image(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(ProviderErrorCodes.BadRequest);
        result.Error.Message.ShouldContain("NSFW");
    }

    /// <summary>BFL modeli.</summary>
    private BflImageModel Bfl() =>
        new(TestFixtures.Model("bfl-flux-3-image-klein"), TestFixtures.Credential(ProviderPlatforms.Bfl), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);

    /// <summary>Replicate modeli.</summary>
    private ReplicateImageModel Replicate() =>
        new(TestFixtures.Model("replicate-flux-schnell"), TestFixtures.Credential(ProviderPlatforms.Replicate), TestFixtures.Dependencies(_recorder), _http.CreateClient(), MediaTestData.NoWait);
}
