using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Image.Tests.ComfyUi;

/// <summary>Gömülü ComfyUI şablonlarının doldurulması.</summary>
public sealed class ComfyUiWorkflowProviderTests
{
    private readonly EmbeddedComfyUiWorkflowProvider _provider = new();

    /// <summary>İki şablon var: z-image-turbo ve flux2-klein-4b.</summary>
    [Fact]
    public void Has_templates_for_both_models()
    {
        _provider.ModelKeys.ShouldBe(["flux2-klein-4b", "z-image-turbo"]);
        _provider.GetTemplate("z-image-turbo").ShouldNotBeNull().DefaultSteps.ShouldBe(8);
        _provider.GetTemplate("flux2-klein-4b").ShouldNotBeNull().DefaultSteps.ShouldBe(4);
    }

    /// <summary>Sayısal yer tutucular sayı olur, metinler yerleşir, hiçbir yer tutucu kalmaz; şablon değişmez.</summary>
    [Theory]
    [InlineData("z-image-turbo", 8)]
    [InlineData("flux2-klein-4b", 4)]
    public void Fills_placeholders_with_typed_values(string modelKey, int defaultSteps)
    {
        var result = _provider.Build(new ImageRequest(modelKey, "deniz \"feneri\"", null, 576, 1024, 123456789012, null, "x.png"), "tk/abc");

        result.IsSuccess.ShouldBeTrue();
        var workflow = result.Value;
        var json = workflow.ToJsonString();
        json.ShouldNotContain("{{");
        var sampler = workflow["8"]!["inputs"]!;
        sampler["seed"]!.GetValueKind().ShouldBe(JsonValueKind.Number);
        sampler["seed"]!.GetValue<long>().ShouldBe(123456789012);
        sampler["steps"]!.GetValue<long>().ShouldBe(defaultSteps);
        workflow["7"]!["inputs"]!["width"]!.GetValue<long>().ShouldBe(576);
        workflow["7"]!["inputs"]!["height"]!.GetValue<long>().ShouldBe(1024);
        workflow["5"]!["inputs"]!["text"]!.GetValue<string>().ShouldBe("deniz \"feneri\"");
        workflow["10"]!["inputs"]!["filename_prefix"]!.GetValue<string>().ShouldBe("tk/abc");
        _provider.GetTemplate(modelKey)!.Workflow.ToJsonString().ShouldContain("{{prompt}}");
    }

    /// <summary>Şablonu olmayan model doğrulama hatası verir.</summary>
    [Fact]
    public void Unknown_model_returns_validation_error()
    {
        var result = _provider.Build(new ImageRequest("qwen-image", "x", null, 512, 512, 1, null, "x.png"), "p");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("comfyui.workflow.missing");
    }

    /// <summary>Negatif tohum ComfyUI için geçerli (≥0) rastgele tohuma çevrilir.</summary>
    [Fact]
    public void Negative_seed_becomes_random_non_negative()
    {
        var result = _provider.Build(new ImageRequest("z-image-turbo", "x", null, 512, 512, -1, 3, "x.png"), "p");

        var seed = result.Value["8"]!["inputs"]!["seed"]!.GetValue<long>();
        seed.ShouldBeGreaterThanOrEqualTo(0);
        result.Value["8"]!["inputs"]!["steps"]!.GetValue<long>().ShouldBe(3);
        result.Value["4"].ShouldBeOfType<JsonObject>();
    }
}
