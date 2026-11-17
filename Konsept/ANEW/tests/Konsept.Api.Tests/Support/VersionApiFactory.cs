using System.Text.Json.Nodes;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Yeni sürüm / yeniden üret testleri (plan 0020): <see cref="PipelineApiFactory"/> (hat açık, bellek depoları, sahte sağlayıcı ve ajan) ve gömülü
/// kataloğa ek bir online ses girişi (ElevenLabs — gerçek katalog girişi B-115 ile gelir; seçenek listesi ve online işareti bununla kanıtlanır).
/// </summary>
public sealed class VersionApiFactory : ProductionApiFactory
{
    /// <summary>Test kataloğundaki online ses modeli.</summary>
    public const string ElevenLabs = "elevenlabs/eleven-multilingual-v2";

    /// <summary>Initializes a new instance of the <see cref="VersionApiFactory"/> class.</summary>
    public VersionApiFactory()
    {
        var seed = JsonNode.Parse(EmbeddedCatalogSource.ReadEmbeddedJson())!;
        seed["providers"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "elevenlabs",
            ["name"] = "ElevenLabs",
            ["validationUrl"] = null,
            ["authStyle"] = "bearer",
            ["dialect"] = "compatible",
            ["chatUrl"] = "https://api.elevenlabs.io/v1/text-to-speech",
        });
        seed["models"]!.AsArray().Add(new JsonObject
        {
            ["id"] = ElevenLabs,
            ["providerId"] = "elevenlabs",
            ["name"] = "ElevenLabs",
            ["kind"] = "audio",
            ["apiModel"] = "eleven_multilingual_v2",
            ["effortLevels"] = new JsonArray(),
            ["timeoutSeconds"] = 600,
        });
        Catalog = EmbeddedCatalogSource.Parse(seed.ToJsonString());
    }

    /// <inheritdoc />
    protected override bool PipelineEnabled => true;
}
