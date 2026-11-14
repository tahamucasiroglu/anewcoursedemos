using System.Collections.Concurrent;
using System.Reflection;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;

namespace TahaKonsept.Agent.Engines.Video.Authoring;

/// <summary>
/// LLM'in sahne kodu yazarken uyması gereken motor sözleşmeleri (gömülü Markdown kaynakları). Sunucu bunları sistem promptuna ekler.
/// Önemli üyeler: <see cref="HyperFrames"/>, <see cref="Remotion"/>, <see cref="Revideo"/>, <see cref="ForEngine"/>.
/// Aynı içerik <c>docs/engines/*-authoring.md</c> altında da durur.
/// </summary>
public static class AuthoringGuides
{
    /// <summary>HyperFrames kılavuzu kaynak adı.</summary>
    public const string HyperFramesResource = "HyperFramesAuthoringGuide.md";

    /// <summary>Remotion kılavuzu kaynak adı.</summary>
    public const string RemotionResource = "RemotionAuthoringGuide.md";

    /// <summary>Revideo kılavuzu kaynak adı.</summary>
    public const string RevideoResource = "RevideoAuthoringGuide.md";

    private const string Prefix = "TahaKonsept.Agent.Engines.Video.Authoring.";

    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

    /// <summary>HyperFrames (HTML + GSAP) yazım kılavuzu.</summary>
    public static string HyperFrames => Load(HyperFramesResource);

    /// <summary>Remotion (TSX) yazım kılavuzu.</summary>
    public static string Remotion => Load(RemotionResource);

    /// <summary>Revideo (TSX, generator) yazım kılavuzu.</summary>
    public static string Revideo => Load(RevideoResource);

    /// <summary>Motor anahtarına göre kılavuzu döner; bilinmeyen anahtarda <c>null</c>.</summary>
    /// <param name="engineKey">Motor anahtarı (<c>hyperframes</c>, <c>remotion</c>, <c>revideo</c>).</param>
    /// <returns>Markdown metni ya da <c>null</c>.</returns>
    public static string? ForEngine(string engineKey) => engineKey.ToLowerInvariant() switch
    {
        HyperFramesVideoEngine.EngineKey => HyperFrames,
        RemotionVideoEngine.EngineKey => Remotion,
        RevideoVideoEngine.EngineKey => Revideo,
        _ => null,
    };

    private static string Load(string resource) => Cache.GetOrAdd(resource, static name =>
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Prefix + name)
            ?? throw new InvalidOperationException($"Gömülü kaynak bulunamadı: {Prefix}{name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}
