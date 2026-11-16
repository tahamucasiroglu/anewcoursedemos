using System.Text.Json;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Model çıktısını ayrıştırır (spec 0014 Req 3, 10, 18; LLM çıktısı güvenilmeyen veridir). Kod çiti ve JSON dışı önsöz/sonsöz atlanır:
/// ilk <c>{</c> ile son <c>}</c> arası okunur. Ayrıştırılamayan ya da alanları eksik çıktı <c>null</c> döner (kaydedilmez, "üretilemedi").
/// Metin değerleri kırpılır; hiçbir değer yorumlanmaz ya da çalıştırılmaz.
/// </summary>
public static class ModelOutputParser
{
    /// <summary>Konsepti ayrıştırır: başlık ve en az bir sahne; her sahnede başlık, ekran tarifi, seslendirme ve pozitif tam saniye.</summary>
    /// <param name="text">Model çıktısı.</param>
    /// <returns>Konsept ya da <c>null</c>.</returns>
    public static ParsedConcept? Concept(string? text)
    {
        using var doc = Parse(text);
        if (doc is null || doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var root = doc.RootElement;
        if (Text(root, "title") is not { } title
            || !root.TryGetProperty("scenes", out var scenes)
            || scenes.ValueKind != JsonValueKind.Array
            || scenes.GetArrayLength() == 0)
        {
            return null;
        }

        var list = new List<ConceptScene>();
        foreach (var scene in scenes.EnumerateArray())
        {
            if (scene.ValueKind != JsonValueKind.Object
                || Text(scene, "title") is not { } sceneTitle
                || Text(scene, "visual") is not { } visual
                || Text(scene, "narration") is not { } narration
                || !scene.TryGetProperty("durationSeconds", out var duration)
                || duration.ValueKind != JsonValueKind.Number
                || !duration.TryGetInt32(out var seconds)
                || seconds <= 0)
            {
                return null;
            }

            list.Add(new ConceptScene(sceneTitle, visual, narration, seconds));
        }

        return new ParsedConcept(title, list);
    }

    /// <summary>Konu önerilerini ayrıştırır; başlığı olmayan öğe biçim dışı sayılır (boş başlıklı aday).</summary>
    /// <param name="text">Model çıktısı.</param>
    /// <returns>Adaylar ya da <c>null</c>.</returns>
    public static IReadOnlyList<ParsedTopic>? Topics(string? text)
    {
        using var doc = Parse(text);
        if (doc is null
            || doc.RootElement.ValueKind != JsonValueKind.Object
            || !doc.RootElement.TryGetProperty("topics", out var topics)
            || topics.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<ParsedTopic>();
        foreach (var item in topics.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                list.Add(new ParsedTopic(string.Empty, string.Empty, true));
                continue;
            }

            var extra = item.EnumerateObject().Any(p => p.Name is not ("title" or "summary"));
            list.Add(new ParsedTopic(Text(item, "title") ?? string.Empty, Text(item, "summary") ?? string.Empty, extra));
        }

        return list;
    }

    /// <summary>Denetim sonucunu ayrıştırır: tekrar olan aday numaraları (1'den).</summary>
    /// <param name="text">Model çıktısı.</param>
    /// <returns>Numaralar ya da <c>null</c>.</returns>
    public static IReadOnlySet<int>? Duplicates(string? text)
    {
        using var doc = Parse(text);
        if (doc is null
            || doc.RootElement.ValueKind != JsonValueKind.Object
            || !doc.RootElement.TryGetProperty("duplicates", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var set = new HashSet<int>();
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var n))
            {
                return null;
            }

            set.Add(n);
        }

        return set;
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()?.Trim() is { Length: > 0 } s
            ? s
            : null;

    private static JsonDocument? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(text.AsMemory(start, end - start + 1));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
