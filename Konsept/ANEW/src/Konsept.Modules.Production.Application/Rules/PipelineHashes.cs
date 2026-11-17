using System.Text.Encodings.Web;
using System.Text.Json;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;

namespace Konsept.Modules.Production.Application.Rules;

/// <summary>
/// Hat özetleri (spec 0017 Req 10, spec 0020): tam özet (metin + çıktıyı etkileyen tüm ayarlar; sıradan iş bununla tümden yeniden kullanır),
/// metin imzası (metin + kod modeli + dil + biçim; aşama imzalarının kökü) ve imzası olmayan eski (0.5.0) çıktılar için imza türetme.
/// </summary>
public static class PipelineHashes
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Tam özet (0017; review r1 M-2): ayar değişirse sıradan işte eski kod/ses/render yeniden kullanılmaz.</summary>
    /// <param name="content">Konsept metni.</param>
    /// <param name="settings">Sabit ayarlar.</param>
    /// <returns>Özet.</returns>
    public static string Full(ConceptContent content, FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(settings);
        return PipelineRules.Hash(JsonSerializer.Serialize(
            new { content.Title, content.Scenes, settings.CodeModel, settings.Language, settings.Format, settings.Engine, settings.ImageModel, settings.AudioModel },
            Json));
    }

    /// <summary>Metin imzası (analiz aşamasının imzası).</summary>
    /// <param name="content">Konsept metni.</param>
    /// <param name="settings">Sabit ayarlar.</param>
    /// <returns>İmza.</returns>
    public static string Text(ConceptContent content, FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(settings);
        return PipelineRules.Hash(JsonSerializer.Serialize(new { content.Title, content.Scenes, settings.CodeModel, settings.Language, settings.Format }, Json));
    }

    /// <summary>Ayarların istenen aşama imzaları.</summary>
    /// <param name="content">Konsept metni.</param>
    /// <param name="settings">Sabit ayarlar.</param>
    /// <returns>Aşama → imza.</returns>
    public static IReadOnlyDictionary<PipelineStage, string> Signatures(ConceptContent content, FixedJobSettings settings) =>
        VersionStages.Signatures(Text(content, settings), settings);

    /// <summary>
    /// İmzasız (0.5.0'da yazılmış) çıktılar taban sürümün tam özetini tutuyorsa imzalar taban ayarlarından türetilir: planı olan analiz, kodu olan
    /// kod, cihazda tamamlanmış yerel aşamalar. Aksi halde çıktılar olduğu gibi döner.
    /// </summary>
    /// <param name="stored">Saklanan çıktılar.</param>
    /// <param name="content">Konsept metni.</param>
    /// <param name="baseSettings">Taban sürümün ayarları.</param>
    /// <returns>Çıktılar.</returns>
    public static StageOutputs? WithLegacySignatures(StageOutputs? stored, ConceptContent content, FixedJobSettings baseSettings)
    {
        if (stored is null || stored.Signatures.Count > 0 || !string.Equals(stored.TextHash, Full(content, baseSettings), StringComparison.Ordinal))
        {
            return stored;
        }

        var derived = new Dictionary<PipelineStage, string>();
        foreach (var (stage, signature) in Signatures(content, baseSettings))
        {
            var present = stage switch
            {
                PipelineStage.Analysis => stored.Plan is not null,
                PipelineStage.Code => stored.Codes is not null,
                _ => stored.LocalDone.Contains(stage),
            };
            if (present)
            {
                derived[stage] = signature;
            }
        }

        return stored with { Signatures = derived };
    }

    /// <summary>İki konsept metni aynı mı (başlık ve sahneler; Req 5 "Konsept değişmedi").</summary>
    /// <param name="a">Birinci.</param>
    /// <param name="b">İkinci.</param>
    /// <returns>Aynıysa <c>true</c>.</returns>
    public static bool SameText(ConceptContent a, ConceptContent b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return string.Equals(a.Title, b.Title, StringComparison.Ordinal) && a.Scenes.SequenceEqual(b.Scenes);
    }
}
