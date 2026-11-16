namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>
/// Çağrının amacı (model rolü, spec 0013 Req 11). <see cref="Trial"/> kullanıcının Ayarlar'dan yaptığı deneme çağrısıdır
/// (plan 0013 Risk 3).
/// </summary>
public enum CallPurpose
{
    /// <summary>Konsept üretimi.</summary>
    ConceptGeneration,

    /// <summary>Denetim.</summary>
    Review,

    /// <summary>Konu önerisi.</summary>
    TopicSuggestion,

    /// <summary>Video kodu.</summary>
    VideoCode,

    /// <summary>Görsel.</summary>
    Image,

    /// <summary>Ses.</summary>
    Voice,

    /// <summary>Deneme çağrısı (Ayarlar > Modeller).</summary>
    Trial,
}
