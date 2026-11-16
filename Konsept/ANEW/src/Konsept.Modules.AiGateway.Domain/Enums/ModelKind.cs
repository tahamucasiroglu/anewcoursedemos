namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Katalog türü (spec 0004 Req 2; "ses" ve "video motoru" spec 0006 Req 18).</summary>
public enum ModelKind
{
    /// <summary>Metin modeli (LLM).</summary>
    Text,

    /// <summary>Görsel modeli.</summary>
    Image,

    /// <summary>Ses (seslendirme) modeli.</summary>
    Audio,

    /// <summary>Video motoru (HyperFrames, Remotion).</summary>
    VideoEngine,
}
