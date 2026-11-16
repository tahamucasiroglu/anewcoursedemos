using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Sürüm listesindeki değişiklik özeti (spec 0007 Req 6): bir sürümü bir öncekiyle karşılaştırır. İlk sürümde yalnızca <see cref="First"/>;
/// fark yoksa boş liste. Anahtarların metni Application resx'indedir.
/// </summary>
public static class VersionChanges
{
    /// <summary>İlk sürüm.</summary>
    public const string First = "first";

    /// <summary>Konsept metni.</summary>
    public const string Concept = "concept";

    /// <summary>Video kodu modeli.</summary>
    public const string CodeModel = "code_model";

    /// <summary>Video motoru.</summary>
    public const string Engine = "engine";

    /// <summary>Görsel modeli.</summary>
    public const string ImageModel = "image_model";

    /// <summary>Ses modeli.</summary>
    public const string AudioModel = "audio_model";

    /// <summary>Altyazı.</summary>
    public const string Subtitles = "subtitles";

    /// <summary>İlerleme çubuğu.</summary>
    public const string ProgressBar = "progress_bar";

    /// <summary>Biçim.</summary>
    public const string Format = "format";

    /// <summary>Değişiklik anahtarları.</summary>
    /// <param name="previous">Önceki sürüm (<c>null</c> = ilk sürüm).</param>
    /// <param name="current">Bu sürüm.</param>
    /// <returns>Anahtarlar, sabit sırada.</returns>
    public static IReadOnlyList<string> Describe(ProducedVideo? previous, ProducedVideo current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (previous is null)
        {
            return [First];
        }

        var changes = new List<string>();
        Add(changes, Concept, !string.Equals(previous.ConceptText, current.ConceptText, StringComparison.Ordinal));
        Add(changes, CodeModel, !string.Equals(previous.CodeModel, current.CodeModel, StringComparison.Ordinal));
        Add(changes, Engine, !string.Equals(previous.Engine, current.Engine, StringComparison.Ordinal));
        Add(changes, ImageModel, !string.Equals(previous.ImageModel, current.ImageModel, StringComparison.Ordinal));
        Add(changes, AudioModel, !string.Equals(previous.AudioModel, current.AudioModel, StringComparison.Ordinal));
        Add(changes, Subtitles, previous.Subtitles != current.Subtitles);
        Add(changes, ProgressBar, previous.ProgressBar != current.ProgressBar);
        Add(changes, Format, !string.Equals(previous.Format, current.Format, StringComparison.Ordinal));
        return changes;
    }

    private static void Add(List<string> changes, string key, bool changed)
    {
        if (changed)
        {
            changes.Add(key);
        }
    }
}
