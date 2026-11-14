namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// LLM efor (düşünme derinliği) seviyesi. Sağlayıcıdaki karşılığı: Anthropic <c>output_config.effort</c>,
/// OpenAI-uyumlu sağlayıcılarda <c>reasoning_effort</c>.
/// </summary>
public enum EffortLevel
{
    /// <summary>Düşük: ucuz ve hızlı (konu önerisi gibi basit işler).</summary>
    Low = 0,

    /// <summary>Orta.</summary>
    Medium = 1,

    /// <summary>Yüksek: kod ve zekâ gerektiren işler.</summary>
    High = 2,

    /// <summary>Çok yüksek (Anthropic <c>xhigh</c>).</summary>
    XHigh = 3,

    /// <summary>En yüksek: maliyet ikinci planda.</summary>
    Max = 4,
}
