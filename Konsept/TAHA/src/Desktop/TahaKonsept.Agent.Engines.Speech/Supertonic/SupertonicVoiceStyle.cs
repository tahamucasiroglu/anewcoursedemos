using System.Text.Json;

namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>
/// Supertonic ses stili (voice_styles/*.json): metin→gizil (style_ttl) ve süre tahmini (style_dp) gömmeleri ve şekilleri.
/// <see cref="Load"/> JSON'u (<c>{"style_ttl":{"dims":[1,a,b],"data":[[[...]]]}, "style_dp":{...}}</c>) düz diziye açar.
/// </summary>
internal sealed class SupertonicVoiceStyle
{
    /// <summary>Stili kurar.</summary>
    /// <param name="ttl">style_ttl verisi (düz).</param>
    /// <param name="ttlShape">style_ttl şekli.</param>
    /// <param name="dp">style_dp verisi (düz).</param>
    /// <param name="dpShape">style_dp şekli.</param>
    public SupertonicVoiceStyle(float[] ttl, int[] ttlShape, float[] dp, int[] dpShape)
    {
        Ttl = ttl;
        TtlShape = ttlShape;
        Dp = dp;
        DpShape = dpShape;
    }

    /// <summary>style_ttl (düz).</summary>
    public float[] Ttl { get; }

    /// <summary>style_ttl şekli ([1, n, d]).</summary>
    public int[] TtlShape { get; }

    /// <summary>style_dp (düz).</summary>
    public float[] Dp { get; }

    /// <summary>style_dp şekli ([1, n, d]).</summary>
    public int[] DpShape { get; }

    /// <summary>Ses stili JSON dosyasını okur.</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>Stil.</returns>
    public static SupertonicVoiceStyle Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var (ttl, ttlShape) = ReadTensor(root.GetProperty("style_ttl"));
        var (dp, dpShape) = ReadTensor(root.GetProperty("style_dp"));
        return new SupertonicVoiceStyle(ttl, ttlShape, dp, dpShape);
    }

    private static (float[] Data, int[] Shape) ReadTensor(JsonElement element)
    {
        var shape = element.GetProperty("dims").EnumerateArray().Select(d => d.GetInt32()).ToArray();
        var data = new List<float>(shape.Aggregate(1, (a, b) => a * b));
        Flatten(element.GetProperty("data"), data);
        return (data.ToArray(), shape);
    }

    private static void Flatten(JsonElement element, List<float> target)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                Flatten(child, target);
            }

            return;
        }

        target.Add(element.GetSingle());
    }
}
