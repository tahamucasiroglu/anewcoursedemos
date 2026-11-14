using System.Text.Json.Nodes;
using Shouldly;
using TahaKonsept.Modules.Providers.Application.Json;
using TahaKonsept.Modules.Providers.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Json;

/// <summary>
/// Şema üretimi (strict kurallar) ve model metninden JSON ayıklama.
/// </summary>
public sealed class JsonSchemaBuilderTests
{
    /// <summary>Nesne şeması strict: additionalProperties false, tüm alanlar required, camelCase.</summary>
    [Fact]
    public void Builds_strict_object_schema()
    {
        var schema = JsonSchemaBuilder.For(typeof(SampleConcept));
        schema["type"]!.GetValue<string>().ShouldBe("object");
        schema["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.ShouldBe(["title", "sceneCount", "tags", "note"], ignoreOrder: true);
        schema["properties"]!["tags"]!["type"].ShouldNotBeNull();
    }

    /// <summary>Nesne olmayan tür reddedilir.</summary>
    [Fact]
    public void Rejects_non_object_types() =>
        Should.Throw<ArgumentException>(() => JsonSchemaBuilder.For(typeof(List<string>)));

    /// <summary>Kod çiti ve açıklama ayıklanır.</summary>
    [Fact]
    public void Parser_strips_fences_and_prose()
    {
        var ok = JsonResponseParser.TryParse<SampleConcept>("İşte:\n```json\n{\"title\":\"Bizans\",\"sceneCount\":5,\"tags\":[\"tarih\"],\"note\":null}\n```", out var value, out _);
        ok.ShouldBeTrue();
        value!.Title.ShouldBe("Bizans");
        value.SceneCount.ShouldBe(5);
        JsonResponseParser.TryParse<SampleConcept>("{\"title\": \"yarım", out _, out var error).ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
        JsonNode.Parse(JsonResponseParser.Extract("x {\"a\":1} y")).ShouldNotBeNull();
    }
}
