namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Katalog türü (spec 0004 Req 2): bu spec'te metin ve görsel; "ses" ve "video motoru" 0006'da eklenir.</summary>
public enum ModelKind
{
    /// <summary>Metin modeli (LLM).</summary>
    Text,

    /// <summary>Görsel modeli.</summary>
    Image,
}
