namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Üretim şeridi (spec 0014 Req 19): konudan (Konu üret, Seçtiklerini üret) ve fikirden üretim aynı anda çalışabilir.</summary>
public enum JobLane
{
    /// <summary>Konudan üret alanı.</summary>
    Topic,

    /// <summary>Fikirden üret alanı.</summary>
    Idea,
}
