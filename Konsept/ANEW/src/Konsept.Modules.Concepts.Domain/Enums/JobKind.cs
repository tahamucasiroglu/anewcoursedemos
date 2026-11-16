namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Üretim işinin türü.</summary>
public enum JobKind
{
    /// <summary>Konu önerisi (Konu üret).</summary>
    Topics,

    /// <summary>Seçilen önerilerden konsept (Seçtiklerini üret).</summary>
    FromTopics,

    /// <summary>Fikirden konsept.</summary>
    FromIdea,
}
