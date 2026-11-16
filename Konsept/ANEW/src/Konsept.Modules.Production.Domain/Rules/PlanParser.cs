using System.Text.Json;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Model çıktılarının ayrıştırılması (spec 0017 Req 7): analiz JSON'u sahne sayısı konseptinkiyle aynı olmalıdır; eksik süre/ses/görsel
/// tarifi konseptin sahnesinden tamamlanır. Video kodu kod çiti varsa ondan arındırılır ve bir HTML belgesi olmalıdır. Biçim dışı çıktı
/// <c>null</c> döner (aşama hata olur).
/// </summary>
public static class PlanParser
{
    /// <summary>Analiz çıktısını ayrıştırır.</summary>
    /// <param name="text">Model metni.</param>
    /// <param name="scenes">Konseptin sahneleri (seslendirme, ekran tarifi, süre).</param>
    /// <returns>Plan ya da <c>null</c>.</returns>
    public static IReadOnlyList<ScenePlan>? ParsePlan(string? text, IReadOnlyList<(string Narration, string Visual, int Duration)> scenes)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        var json = Unfence(text);
        if (json is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("scenes", out var list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() != scenes.Count)
            {
                return null;
            }

            var plan = new List<ScenePlan>(scenes.Count);
            var i = 0;
            foreach (var item in list.EnumerateArray())
            {
                var source = scenes[i];
                var seconds = item.TryGetProperty("durationSeconds", out var d) && d.ValueKind == JsonValueKind.Number && d.TryGetInt32(out var n) && n > 0
                    ? n
                    : Math.Max(1, source.Duration);
                plan.Add(new ScenePlan(i, seconds, Text(item, "narration") ?? source.Narration, Text(item, "imagePrompt") ?? source.Visual));
                i++;
            }

            return plan;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Video kodu çıktısını ayrıştırır.</summary>
    /// <param name="text">Model metni.</param>
    /// <returns>HTML ya da <c>null</c>.</returns>
    public static string? ParseCode(string? text)
    {
        var code = Unfence(text);
        return code is not null && code.Contains("<html", StringComparison.OrdinalIgnoreCase) ? code : null;
    }

    private static string? Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()) ? v.GetString()!.Trim() : null;

    private static string? Unfence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var t = text.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = t.IndexOf('\n', StringComparison.Ordinal);
            var end = t.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine < 0 || end <= firstLine)
            {
                return null;
            }

            t = t[(firstLine + 1)..end].Trim();
        }

        return t.Length == 0 ? null : t;
    }
}
