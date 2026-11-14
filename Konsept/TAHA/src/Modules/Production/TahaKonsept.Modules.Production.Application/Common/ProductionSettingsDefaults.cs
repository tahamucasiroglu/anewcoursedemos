using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Üretim ayarlarının varsayılanları ve düzeltmesi: <see cref="Normalize"/> boş/aralık dışı değerleri güvenli varsayılana çeker
/// (motor küçük harf ve bilinen, fps 15-60, hız 0.5-2, model anahtarları kırpılır); <see cref="Resolution"/> biçimden çözünürlük verir.
/// </summary>
public static class ProductionSettingsDefaults
{
    /// <summary>Varsayılan kod modeli.</summary>
    public const string CodeModelKey = "claude-opus-5-5";

    /// <summary>Varsayılan motor.</summary>
    public const string Engine = "hyperframes";

    /// <summary>Ayarları düzeltir.</summary>
    /// <param name="settings">Ayarlar (<c>null</c> = varsayılan).</param>
    /// <returns>Düzeltilmiş ayarlar.</returns>
    public static ProductionSettingsDto Normalize(ProductionSettingsDto? settings)
    {
        var s = settings ?? new ProductionSettingsDto();
        var engine = (s.VideoEngine ?? string.Empty).Trim().ToLowerInvariant();
        return s with
        {
            CodeModelKey = string.IsNullOrWhiteSpace(s.CodeModelKey) ? CodeModelKey : s.CodeModelKey.Trim(),
            VideoEngine = PipelineStages.KnownEngines.Contains(engine) ? engine : Engine,
            ImageModelKey = string.IsNullOrWhiteSpace(s.ImageModelKey) ? null : s.ImageModelKey.Trim(),
            SpeechModelKey = string.IsNullOrWhiteSpace(s.SpeechModelKey) ? null : s.SpeechModelKey.Trim(),
            VoiceId = string.IsNullOrWhiteSpace(s.VoiceId) ? null : s.VoiceId.Trim(),
            SpeechSpeed = s.SpeechSpeed is >= 0.5 and <= 2.0 ? Math.Round(s.SpeechSpeed, 2) : 1.0,
            Fps = s.Fps is >= 15 and <= 60 ? s.Fps : 30,
            NormalizeBySpeechModel = s.NormalizeBySpeechModel ?? new Dictionary<string, bool>(),
        };
    }

    /// <summary>Biçime göre çözünürlük (dikey 1080x1920, yatay 1920x1080).</summary>
    /// <param name="format">Biçim.</param>
    /// <returns>Genişlik ve yükseklik.</returns>
    public static (int Width, int Height) Resolution(VideoFormat format) =>
        format == VideoFormat.Horizontal ? (1920, 1080) : (1080, 1920);
}
