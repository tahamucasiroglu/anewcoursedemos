namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Konseptin yaşam döngüsü durumu (BR-16, spec 0014 Req 15, 24).</summary>
public enum ConceptStatus
{
    /// <summary>Üretildi.</summary>
    Generated = 0,

    /// <summary>Onaylandı (video üretimi için).</summary>
    Approved = 1,

    /// <summary>Sırada (0006).</summary>
    Queued = 2,

    /// <summary>Üretiliyor (0017).</summary>
    Producing = 3,

    /// <summary>Video oldu.</summary>
    VideoReady = 4,
}
