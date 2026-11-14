using System.Text.Json;
using Shouldly;
using TahaKonsept.Agent.Engines.Speech.Python.Protocol;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Python;

/// <summary>JSON-lines protokolü serileştirme/ayrıştırma ve <c>TtsWorkerClientBase</c> davranışı testleri.</summary>
public sealed class WorkerProtocolTests
{
    /// <summary>Synth isteği snake_case alanlarla, boşlar atlanarak, Türkçe karakterler kaçışsız tek satır yazılır.</summary>
    [Fact]
    public void Synth_request_serializes_to_single_snake_case_line()
    {
        var json = WorkerProtocol.Serialize(WorkerProtocol.Synth("İstanbul'u fethetti.", "tr", null, "C:/ref.wav", 1.1, "C:/o.wav"));

        json.ShouldNotContain("\n");
        json.ShouldContain("\"text\":\"İstanbul'u fethetti.\"");
        json.ShouldContain("\"ref_audio\":\"C:/ref.wav\"");
        json.ShouldNotContain("\"voice\"");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("cmd").GetString().ShouldBe("synth");
        doc.RootElement.GetProperty("lang").GetString().ShouldBe("tr");
        doc.RootElement.GetProperty("speed").GetDouble().ShouldBe(1.1);
        doc.RootElement.GetProperty("out").GetString().ShouldBe("C:/o.wav");
    }

    /// <summary>Load isteği engine/model/device/options taşır; boş options yazılmaz.</summary>
    [Fact]
    public void Load_request_carries_engine_model_device_and_options()
    {
        var withOptions = WorkerProtocol.Serialize(WorkerProtocol.Load("fish", "fishaudio/s2-pro", "cuda", new Dictionary<string, object?> { ["repo_dir"] = "C:/src" }));
        var without = WorkerProtocol.Serialize(WorkerProtocol.Load("dummy", null, "cpu", new Dictionary<string, object?>()));

        withOptions.ShouldBe("{\"cmd\":\"load\",\"engine\":\"fish\",\"model\":\"fishaudio/s2-pro\",\"device\":\"cuda\",\"options\":{\"repo_dir\":\"C:/src\"}}");
        without.ShouldBe("{\"cmd\":\"load\",\"engine\":\"dummy\",\"device\":\"cpu\"}");
        WorkerProtocol.Serialize(WorkerProtocol.Ping()).ShouldBe("{\"cmd\":\"ping\"}");
    }

    /// <summary>Başarılı ve hatalı yanıtlar ayrıştırılır.</summary>
    [Fact]
    public void Parses_success_and_error_responses()
    {
        WorkerProtocol.TryParseResponse("{\"ok\": true, \"out\": \"C:/a.wav\", \"duration\": 3.25, \"sample_rate\": 24000}", out var ok).ShouldBeTrue();
        ok!.Ok.ShouldBeTrue();
        ok.Out.ShouldBe("C:/a.wav");
        ok.Duration.ShouldBe(3.25);
        ok.SampleRate.ShouldBe(24000);

        WorkerProtocol.TryParseResponse("{\"ok\": false, \"error\": \"ValueError: x\"}", out var error).ShouldBeTrue();
        error!.Ok.ShouldBeFalse();
        error.Error.ShouldBe("ValueError: x");
    }

    /// <summary>Log satırları, bozuk JSON ve <c>ok</c> alanı olmayan nesneler yanıt sayılmaz.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Downloading model.safetensors: 45%")]
    [InlineData("{bozuk json")]
    [InlineData("{\"progress\": 0.5}")]
    [InlineData("{\"ok\": \"evet\"}")]
    [InlineData("[1,2,3]")]
    public void Non_protocol_lines_are_ignored(string line)
    {
        WorkerProtocol.TryParseResponse(line, out var response).ShouldBeFalse();
        response.ShouldBeNull();
    }

    /// <summary>İstemci, yanıt öncesi log satırlarını atlar ve isteği tek satır yazar.</summary>
    [Fact]
    public async Task Client_skips_log_lines_until_response()
    {
        await using var client = new InMemoryTtsWorkerClient("loading...\nWARNING: x\n{\"ok\": true, \"pong\": true}\n");

        var result = await client.SendAsync(WorkerProtocol.Ping(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Pong.ShouldBe(true);
        client.Sent.ToString().Trim().ShouldBe("{\"cmd\":\"ping\"}");
    }

    /// <summary>Çağıran istek sürerken iptal ederse işçi yeniden başlatılır (geç gelen yanıt sonraki isteğe karışmasın) ve iptal yayılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Caller_cancellation_mid_request_restarts_worker()
    {
        await using var client = new HangingTtsWorkerClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(() => client.SendAsync(WorkerProtocol.Ping(), TimeSpan.FromSeconds(30), cts.Token));

        client.BrokenCount.ShouldBe(1);
    }

    /// <summary>Çıkış yanıt gelmeden kapanırsa iletişim hatası döner.</summary>
    [Fact]
    public async Task Client_reports_failure_when_output_ends()
    {
        await using var client = new InMemoryTtsWorkerClient("Traceback...\n");

        var result = await client.SendAsync(WorkerProtocol.Ping(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("speech.worker");
    }
}
