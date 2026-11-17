using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Yeni sürümün aşama kuralları (spec 0020 Req 1, BR-9): hangi ayar değişikliği hangi aşamaları yeniden üretir (tablo), hangi aşama online
/// çağrı yapar, aşama imzaları (tamamlanan aşamanın girdilerinin özeti) ve çalışacak küme (etkilenenler ∪ çıktısı geçerli olmayanlar).
/// Birleştirme her sürümde çalışır (video dosyası onunla oluşur).
/// </summary>
public static class VersionStages
{
    /// <summary>
    /// Req 1 tablosu: ses motoru → seslendirme (ses açıksa) + birleştirme · video motoru → video kodu + render + birleştirme · görsel modeli →
    /// görsel + render + birleştirme · altyazı / ilerleme çubuğu / sessize geçiş → yalnızca birleştirme. Birden fazla değişiklikte birleşim;
    /// hiçbir ayar değişmediyse boş.
    /// </summary>
    /// <param name="from">Taban sürümün ayarları.</param>
    /// <param name="to">Yeni ayarlar.</param>
    /// <returns>Aşamalar, çalışma sırasıyla.</returns>
    public static IReadOnlyList<PipelineStage> Affected(FixedJobSettings from, FixedJobSettings to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        var set = new HashSet<PipelineStage>();
        if (!Same(from.Engine, to.Engine))
        {
            set.UnionWith([PipelineStage.Code, PipelineStage.Render, PipelineStage.Merge]);
        }

        if (!Same(from.ImageModel, to.ImageModel))
        {
            set.UnionWith([PipelineStage.Images, PipelineStage.Render, PipelineStage.Merge]);
        }

        if (!Same(from.AudioModel, to.AudioModel))
        {
            if (to.AudioModel is not null)
            {
                set.Add(PipelineStage.Voice);
            }

            set.Add(PipelineStage.Merge);
        }

        if (from.Subtitles != to.Subtitles || from.ProgressBar != to.ProgressBar)
        {
            set.Add(PipelineStage.Merge);
        }

        return Ordered(set);
    }

    /// <summary>Aşama bu ayarlarla online model çağrısı yapar mı: analiz ve video kodu her zaman; görsel/ses yalnızca online model seçiliyse.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Online ise <c>true</c>.</returns>
    public static bool IsOnline(PipelineStage stage, FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return stage switch
        {
            PipelineStage.Analysis or PipelineStage.Code => true,
            PipelineStage.Images => settings.ImageModel is not null && !PipelineRules.IsLocal(settings.ImageModel),
            PipelineStage.Voice => settings.AudioModel is not null && !PipelineRules.IsLocal(settings.AudioModel),
            _ => false,
        };
    }

    /// <summary>Aşamalardan en az biri online çağrı yapar mı (Req 3).</summary>
    /// <param name="stages">Aşamalar.</param>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Online ise <c>true</c>.</returns>
    public static bool Online(IEnumerable<PipelineStage> stages, FixedJobSettings settings) => stages.Any(s => IsOnline(s, settings));

    /// <summary>
    /// İstenen aşama imzaları: analiz = metin imzası (konsept metni + kod modeli + dil + biçim); kod = analiz + motor + görsel var/yok; görsel =
    /// analiz + görsel modeli; ses = analiz + ses modeli; render = kod + görsel. Kapalı görsel/ses için imza yoktur.
    /// </summary>
    /// <param name="textSignature">Metin imzası.</param>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Aşama → imza.</returns>
    public static IReadOnlyDictionary<PipelineStage, string> Signatures(string textSignature, FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(textSignature);
        ArgumentNullException.ThrowIfNull(settings);
        var code = PipelineRules.Hash("code|" + textSignature + "|" + settings.Engine + "|" + (settings.ImageModel is not null ? "img" : "noimg"));
        var images = settings.ImageModel is null ? null : PipelineRules.Hash("images|" + textSignature + "|" + settings.ImageModel);
        var result = new Dictionary<PipelineStage, string>
        {
            [PipelineStage.Analysis] = textSignature,
            [PipelineStage.Code] = code,
            [PipelineStage.Render] = PipelineRules.Hash("render|" + code + "|" + (images ?? "none")),
        };
        if (images is not null)
        {
            result[PipelineStage.Images] = images;
        }

        if (settings.AudioModel is not null)
        {
            result[PipelineStage.Voice] = PipelineRules.Hash("voice|" + textSignature + "|" + settings.AudioModel);
        }

        return result;
    }

    /// <summary>Aşamanın çıktısı yeniden kullanılabilir mi: imza istenenle aynı; yerel aşamada ayrıca aynı ajan cihazında tamamlanmış.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="outputs">Saklanan çıktılar.</param>
    /// <param name="desired">İstenen imzalar.</param>
    /// <param name="device">Ajan cihazı (yerel aşamalar için) ya da <c>null</c>.</param>
    /// <returns>Kullanılabilirse <c>true</c>.</returns>
    public static bool Reusable(PipelineStage stage, StageOutputs? outputs, IReadOnlyDictionary<PipelineStage, string> desired, Guid? device)
    {
        ArgumentNullException.ThrowIfNull(desired);
        if (outputs is null || stage == PipelineStage.Merge || !desired.TryGetValue(stage, out var want)
            || !outputs.Signatures.TryGetValue(stage, out var have) || !string.Equals(want, have, StringComparison.Ordinal))
        {
            return false;
        }

        return stage switch
        {
            PipelineStage.Analysis => outputs.Plan is not null,
            PipelineStage.Code => outputs.Codes is not null,
            _ => device is not null && outputs.LocalDevice == device && outputs.LocalDone.Contains(stage),
        };
    }

    /// <summary>
    /// Çalışacak aşamalar: etkilenenler ∪ açık olup çıktısı yeniden kullanılamayanlar ∪ bunların bağımlıları ∪ birleştirme (kapalı görsel/ses
    /// hiç çalışmaz).
    /// </summary>
    /// <param name="settings">Yeni ayarlar.</param>
    /// <param name="outputs">Saklanan çıktılar.</param>
    /// <param name="desired">İstenen imzalar.</param>
    /// <param name="device">Ajan cihazı.</param>
    /// <param name="affected">Etkilenen aşamalar.</param>
    /// <returns>Aşamalar, çalışma sırasıyla.</returns>
    public static IReadOnlyList<PipelineStage> RunSet(
        FixedJobSettings settings, StageOutputs? outputs, IReadOnlyDictionary<PipelineStage, string> desired, Guid? device, IEnumerable<PipelineStage> affected)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(affected);
        var set = new HashSet<PipelineStage>(affected) { PipelineStage.Merge };
        foreach (var stage in PipelineRules.Order)
        {
            if (!Reusable(stage, outputs, desired, device))
            {
                set.Add(stage);
            }
        }

        set.RemoveWhere(s => Off(s, settings)); // Kapalı görsel/ses hiç çalışmaz (bağımlılarını da tetiklemez).
        foreach (var stage in PipelineRules.Order.Where(set.Contains).ToList())
        {
            set.UnionWith(Dependents(stage)); // Yeniden üretilen aşamanın bağımlıları da yeniden üretilir (hat aynı kuralı uygular).
        }

        set.RemoveWhere(s => Off(s, settings));
        return Ordered(set);
    }

    /// <summary>Tüm aşamalar (yeniden üretim), kapalı görsel/ses hariç.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Aşamalar.</returns>
    public static IReadOnlyList<PipelineStage> All(FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return [.. PipelineRules.Order.Where(s => !Off(s, settings))];
    }

    /// <summary>Aşama yeniden üretilince çıktısı geçersizleşen aşamalar: analiz → hepsi; kod ve görsel → render.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Bağımlılar.</returns>
    public static IReadOnlyList<PipelineStage> Dependents(PipelineStage stage) => stage switch
    {
        PipelineStage.Analysis => [PipelineStage.Code, PipelineStage.Images, PipelineStage.Voice, PipelineStage.Render],
        PipelineStage.Code or PipelineStage.Images => [PipelineStage.Render],
        _ => [],
    };

    private static bool Off(PipelineStage stage, FixedJobSettings settings) =>
        (stage == PipelineStage.Images && settings.ImageModel is null) || (stage == PipelineStage.Voice && settings.AudioModel is null);

    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);

    private static List<PipelineStage> Ordered(HashSet<PipelineStage> set) => [.. PipelineRules.Order.Where(set.Contains)];
}
