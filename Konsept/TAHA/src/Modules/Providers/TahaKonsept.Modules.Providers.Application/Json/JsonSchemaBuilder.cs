using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Modules.Providers.Application.Json;

/// <summary>
/// Bir .NET türünden sağlayıcıların "strict" yapılandırılmış çıktı kurallarına uygun JSON şeması üretir:
/// kök nesne, her nesnede <c>additionalProperties: false</c> ve tüm özellikler <c>required</c> (null olabilenler tür dizisiyle).
/// Serileştirme ayarı <see cref="ContractJson.Options"/> (camelCase, enum metin) ile aynıdır.
/// </summary>
public static class JsonSchemaBuilder
{
    /// <summary>Türün şemasını üretir.</summary>
    /// <param name="type">Tür (nesne olmalı).</param>
    /// <returns>Şema.</returns>
    /// <exception cref="ArgumentException">Tür bir JSON nesnesi değilse.</exception>
    public static JsonObject For(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var node = ContractJson.Options.GetJsonSchemaAsNode(
            type,
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });
        if (node is not JsonObject root || !IsObjectSchema(root))
        {
            throw new ArgumentException($"Yapılandırılmış çıktı türü bir JSON nesnesi olmalı: {type.Name}", nameof(type));
        }

        root["type"] = "object";
        Normalize(root);
        return root;
    }

    /// <summary>Şemanın nesne olup olmadığı.</summary>
    /// <param name="schema">Şema düğümü.</param>
    private static bool IsObjectSchema(JsonObject schema) =>
        schema["properties"] is JsonObject
        || schema["type"] is JsonValue v && v.TryGetValue<string>(out var t) && t == "object"
        || schema["type"] is JsonArray a && a.Any(x => x?.GetValue<string>() == "object");

    /// <summary>Özyineli olarak nesneleri strict kurala çeker ve desteklenmeyen anahtarları atar.</summary>
    /// <param name="node">Düğüm.</param>
    private static void Normalize(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                obj.Remove("$schema");
                if (obj["properties"] is JsonObject props)
                {
                    obj["additionalProperties"] = false;
                    obj["required"] = new JsonArray(props.Select(p => (JsonNode?)JsonValue.Create(p.Key)).ToArray());
                }

                foreach (var child in obj.Select(p => p.Value).ToList())
                {
                    Normalize(child);
                }

                break;
            case JsonArray arr:
                foreach (var child in arr.ToList())
                {
                    Normalize(child);
                }

                break;
        }
    }
}
