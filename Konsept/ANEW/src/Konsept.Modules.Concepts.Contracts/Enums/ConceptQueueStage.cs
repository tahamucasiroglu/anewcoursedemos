namespace Konsept.Modules.Concepts.Contracts.Enums;

/// <summary>Konseptin durumu, başka modüllere açık biçimiyle (BR-16; spec 0006). Değerler Concepts'teki durumla birebir.</summary>
public enum ConceptQueueStage
{
    /// <summary>Üretildi.</summary>
    Generated = 0,

    /// <summary>Onaylandı.</summary>
    Approved = 1,

    /// <summary>Sırada.</summary>
    Queued = 2,

    /// <summary>Üretiliyor.</summary>
    Producing = 3,

    /// <summary>Video oldu.</summary>
    VideoReady = 4,
}
