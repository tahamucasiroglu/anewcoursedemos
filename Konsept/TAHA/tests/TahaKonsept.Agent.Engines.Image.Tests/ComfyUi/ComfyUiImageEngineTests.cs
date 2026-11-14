using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.ComfyUi;

/// <summary>ComfyUI motoru: sahte sunucu ile /prompt → /history → /view akışı, hata ve ulaşılamama durumları.</summary>
public sealed class ComfyUiImageEngineTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
    private readonly TempDirectory _temp = new();

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Kuyruğa ekler, iki yoklamada biter, görseli indirir.</summary>
    [Fact]
    public async Task Generates_via_prompt_history_and_view()
    {
        JsonNode? posted = null;
        var historyCalls = 0;
        var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/prompt")
            {
                posted = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                return Json("""{"prompt_id":"p-1","number":1,"node_errors":{}}""");
            }

            if (path == "/history/p-1")
            {
                return ++historyCalls < 2
                    ? Json("{}")
                    : Json("""{"p-1":{"status":{"status_str":"success","completed":true,"messages":[]},"outputs":{"10":{"images":[{"filename":"tk_00001_.png","subfolder":"tahakonsept","type":"output"}]}}}}""");
            }

            if (path == "/view")
            {
                request.RequestUri.Query.ShouldContain("filename=tk_00001_.png");
                request.RequestUri.Query.ShouldContain("subfolder=tahakonsept");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var engine = EngineFactory.CreateComfy(new FakeAgentPaths(_temp.Path), handler);
        var output = Path.Combine(_temp.Path, "out", "c.png");

        var result = await engine.GenerateAsync(new ImageRequest("z-image-turbo", "fener", null, 512, 768, 5, null, output), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        (await File.ReadAllBytesAsync(output, TestContext.Current.CancellationToken)).ShouldBe(Png);
        posted.ShouldNotBeNull()["prompt"]!["5"]!["inputs"]!["text"]!.GetValue<string>().ShouldBe("fener");
        historyCalls.ShouldBe(2);
    }

    /// <summary>execution_error mesajı hata olarak döner.</summary>
    [Fact]
    public async Task Maps_execution_error()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/prompt" => Json("""{"prompt_id":"p-2"}"""),
            "/history/p-2" => Json("""{"p-2":{"status":{"status_str":"error","completed":false,"messages":[["execution_start",{}],["execution_error",{"exception_message":"Allocation on device"}]]},"outputs":{}}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var engine = EngineFactory.CreateComfy(new FakeAgentPaths(_temp.Path), handler);

        var result = await engine.GenerateAsync(new ImageRequest("flux2-klein-4b", "x", null, 512, 512, 1, null, Path.Combine(_temp.Path, "x.png")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("comfyui.execution");
        result.Error.Message.ShouldContain("Allocation on device");
    }

    /// <summary>Düğüm hatasıyla reddedilen iş akışı (HTTP 400) anlaşılır hata verir.</summary>
    [Fact]
    public async Task Maps_rejected_prompt()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"type":"prompt_outputs_failed_validation"},"node_errors":{"1":{"errors":[{"message":"Value not in list"}]}}}"""),
        });
        var engine = EngineFactory.CreateComfy(new FakeAgentPaths(_temp.Path), handler);

        var result = await engine.GenerateAsync(new ImageRequest("z-image-turbo", "x", null, 512, 512, 1, null, Path.Combine(_temp.Path, "x.png")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("comfyui.prompt.rejected");
        result.Error.Message.ShouldContain("Value not in list");
    }

    /// <summary>IsInstalled = sunucu ulaşılabilir; yalnızca şablonu olan modelleri destekler.</summary>
    [Fact]
    public async Task Is_installed_reflects_reachability()
    {
        var up = EngineFactory.CreateComfy(new FakeAgentPaths(_temp.Path), new StubHttpMessageHandler(_ => Json("{}")));
        var down = EngineFactory.CreateComfy(new FakeAgentPaths(_temp.Path), new StubHttpMessageHandler(_ => throw new HttpRequestException("bağlantı reddedildi")));

        up.IsInstalled().ShouldBeTrue();
        up.SupportedModels.ShouldBe(["flux2-klein-4b", "z-image-turbo"]);
        down.IsInstalled().ShouldBeFalse();
        var ready = await down.EnsureReadyAsync(null, TestContext.Current.CancellationToken);
        ready.IsFailure.ShouldBeTrue();
        ready.Error.Message.ShouldContain("127.0.0.1:8188");
        down.Key.ShouldBe("comfyui");
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
