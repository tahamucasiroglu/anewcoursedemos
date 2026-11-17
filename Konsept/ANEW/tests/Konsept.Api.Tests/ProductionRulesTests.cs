using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>"Sırayla üret" engelleri ve varsayılanlar (spec 0006 Req 2, 6, 14; saf kurallar).</summary>
public sealed class ProductionRulesTests
{
    private static readonly SelectableModel[] Models =
    [
        new("anthropic/claude-sonnet-5-5", "text", "available", null),
        new("openai/gpt-6.1-terra", "text", "no_key", null),
        new("local/hyperframes", "video_engine", "available", null),
        new("local/supertonic-3", "audio", "available", "tr"),
        new("local/z-image-turbo", "image", "available", "en"),
    ];

    /// <summary>Neden sırası: çalışan iş, boş sıra, liste yok, kod modeli yok, model yok, yetki, key, dil; hepsi uygunsa <c>null</c>.</summary>
    [Fact]
    public void StartBlock_Order_And_Language()
    {
        var ok = Settings("anthropic/claude-sonnet-5-5", audio: null);
        StartBlock.Evaluate(ok, Models, ["en"], runningHere: true, runningElsewhere: true).ShouldBe(StartBlock.RunningHere);
        StartBlock.Evaluate(ok, Models, ["en"], false, true).ShouldBe(StartBlock.RunningElsewhere);
        StartBlock.Evaluate(ok, Models, [], false, false).ShouldBe(StartBlock.Empty);
        StartBlock.Evaluate(ok, null, ["en"], false, false).ShouldBe(StartBlock.ModelsFailed);
        StartBlock.Evaluate(Settings(null), Models, ["en"], false, false).ShouldBe(StartBlock.NoCodeModel);
        StartBlock.Evaluate(Settings("gone/model"), Models, ["en"], false, false).ShouldBe(StartBlock.Unavailable);
        StartBlock.Evaluate(Settings("openai/gpt-6.1-terra", audio: null), Models, ["en"], false, false).ShouldBe(StartBlock.NoKey);
        StartBlock.Evaluate(ok, [.. Models.Select(m => m with { Availability = m.Id == "local/hyperframes" ? "no_permission" : m.Availability })], ["en"], false, false)
            .ShouldBe(StartBlock.NoPermission);
        StartBlock.Evaluate(Settings("anthropic/claude-sonnet-5-5"), Models, ["tr", "en"], false, false).ShouldBe(StartBlock.AudioLanguage);
        StartBlock.Evaluate(Settings("anthropic/claude-sonnet-5-5", image: "local/z-image-turbo", audio: null), Models, ["tr"], false, false)
            .ShouldBe(StartBlock.ImageLanguage);
        StartBlock.Evaluate(Settings("anthropic/claude-sonnet-5-5"), Models, ["TR"], false, false).ShouldBeNull();
        StartBlock.LanguageWarnings(Settings("x", image: "local/z-image-turbo"), Models, ["de"]).ShouldBe([StartBlock.AudioLanguage, StartBlock.ImageLanguage]);
    }

    /// <summary>Varsayılanlar: HyperFrames, Supertonic 3, görsel yalnızca Z-Image-Turbo kuruluysa; Remotion hiçbir zaman varsayılan değil.</summary>
    [Fact]
    public void Defaults_ImageOnlyWhenInstalled()
    {
        var none = ProductionDefaults.For(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string>());
        (none.Engine, none.AudioModel, none.ImageModel, none.CodeModel).ShouldBe(("local/hyperframes", "local/supertonic-3", (string?)null, (string?)null));
        ProductionDefaults.For(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string> { "z-image-turbo", "remotion" }).ShouldSatisfyAllConditions(
            s => s.ImageModel.ShouldBe("local/z-image-turbo"),
            s => s.Engine.ShouldBe("local/hyperframes"));
    }

    private static ProductionSettings Settings(string? code, string? image = null, string? audio = "local/supertonic-3") =>
        new() { Engine = "local/hyperframes", CodeModel = code, ImageModel = image, AudioModel = audio };
}
