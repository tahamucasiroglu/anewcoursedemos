using System.Security.Cryptography;
using System.Text;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Üretim hattının kuralları (spec 0017): aşama sırası, kapalı model → "atlandı" (Req 6, AC-3, AC-4), yeniden kullanılan çıktılar (Req 9, 10,
/// 23: tamamlanmış aşama yeniden çalışmaz; yerel aşamalar yalnızca aynı ajan cihazında geçerli), ajandan istenen araçlar (Req 14), art arda
/// hata sınırı (Req 11), metin özeti. Yerel model kimlikleri <c>local/</c> önekiyle başlar; aracın kimliği önekten sonrasıdır.
/// </summary>
public static class PipelineRules
{
    /// <summary>Sırayı durduran art arda hata sayısı (Req 11).</summary>
    public const int MaxConsecutiveFailures = 3;

    /// <summary>Yerel model öneki.</summary>
    public const string LocalPrefix = "local/";

    /// <summary>Her üretimde gereken çekirdek araçlar (0009 kurulumu).</summary>
    public static readonly IReadOnlyList<string> CoreTools = ["node", "ffmpeg", "chromium"];

    /// <summary>Aşamaların çalışma sırası.</summary>
    public static readonly IReadOnlyList<PipelineStage> Order =
        [PipelineStage.Analysis, PipelineStage.Code, PipelineStage.Images, PipelineStage.Voice, PipelineStage.Render, PipelineStage.Merge];

    /// <summary>Ajanda çalışan (çıktısı ajanda kalan) aşamalar.</summary>
    public static readonly IReadOnlyList<PipelineStage> LocalStages = [PipelineStage.Images, PipelineStage.Voice, PipelineStage.Render, PipelineStage.Merge];

    private static readonly Dictionary<string, string> ToolNames = new(StringComparer.Ordinal)
    {
        ["agent"] = "ajan sürümü (ajanı güncelleyin)",
        ["node"] = "Node.js",
        ["ffmpeg"] = "FFmpeg",
        ["chromium"] = "Chromium",
        ["hyperframes"] = "HyperFrames",
        ["remotion"] = "Remotion",
        ["supertonic-3"] = "Supertonic 3",
        ["chatterbox-multilingual-v3"] = "Chatterbox",
        ["z-image-turbo"] = "Z-Image-Turbo",
        ["flux2-klein-4b"] = "FLUX.2 klein 4B",
    };

    /// <summary>Model yerel mi.</summary>
    /// <param name="modelId">Model.</param>
    /// <returns>Yerelse <c>true</c>.</returns>
    public static bool IsLocal(string? modelId) => modelId is not null && modelId.StartsWith(LocalPrefix, StringComparison.Ordinal);

    /// <summary>Ajandan istenen araçlar (Req 14): çekirdek + motor + seçili yerel görsel/ses modelleri.</summary>
    /// <param name="settings">İşe sabitlenen ayarlar.</param>
    /// <returns>Araç kimlikleri.</returns>
    public static IReadOnlyList<string> RequiredTools(FixedJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var tools = new List<string>(CoreTools);
        foreach (var model in new[] { settings.Engine, settings.ImageModel, settings.AudioModel })
        {
            if (IsLocal(model))
            {
                tools.Add(model![LocalPrefix.Length..]);
            }
        }

        return tools;
    }

    /// <summary>Aracın görünen adı.</summary>
    /// <param name="toolId">Araç.</param>
    /// <returns>Ad.</returns>
    public static string ToolName(string toolId) => ToolNames.TryGetValue(toolId, out var name) ? name : toolId;

    /// <summary>
    /// İşin başlangıç ilerlemesi: kapalı görsel/ses "atlandı"; metin özeti tutan çıktılarda analiz/kod "tamamlandı"; aynı cihazda tamamlanan
    /// yerel aşamalar "tamamlandı" (Req 9, 10, 23). Birleştirme hep yeniden çalışır (video kaydı onunla oluşur).
    /// </summary>
    /// <param name="settings">Sabit ayarlar.</param>
    /// <param name="outputs">Geçerli çıktılar (metin özeti tutan) ya da <c>null</c>.</param>
    /// <param name="device">Şu anki ajan cihazı ya da <c>null</c>.</param>
    /// <returns>Aşamalar.</returns>
    public static IReadOnlyList<StageProgress> Initial(FixedJobSettings settings, StageOutputs? outputs, Guid? device)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return [.. Order.Select(stage => new StageProgress(stage, InitialState(stage, settings, outputs, device)))];
    }

    /// <summary>Konsept metninin özeti (değişirse tüm çıktılar geçersiz, Req 10).</summary>
    /// <param name="canonicalText">Konseptin kararlı metin biçimi.</param>
    /// <returns>Küçük harf onaltılık SHA-256.</returns>
    public static string Hash(string canonicalText)
    {
        ArgumentNullException.ThrowIfNull(canonicalText);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText)));
    }

    /// <summary>Hatadan sonra yeni art arda hata sayısı ve sıranın durup durmayacağı (Req 11).</summary>
    /// <param name="previous">Önceki sayı.</param>
    /// <returns>Yeni sayı ve durma.</returns>
    public static (int Count, bool Halt) AfterFailure(int previous) =>
        (previous + 1, previous + 1 >= MaxConsecutiveFailures);

    private static StageState InitialState(PipelineStage stage, FixedJobSettings settings, StageOutputs? outputs, Guid? device)
    {
        if ((stage == PipelineStage.Images && settings.ImageModel is null) || (stage == PipelineStage.Voice && settings.AudioModel is null))
        {
            return StageState.Skipped;
        }

        var done = stage switch
        {
            PipelineStage.Analysis => outputs?.Plan is not null,
            PipelineStage.Code => outputs?.Codes is not null,
            PipelineStage.Merge => false,
            _ => outputs is not null && device is not null && outputs.LocalDevice == device && outputs.LocalDone.Contains(stage),
        };
        return done ? StageState.Done : StageState.Pending;
    }
}
