namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin nereden üretildiği: <see cref="Topic"/> (önerilen başlıktan), <see cref="Idea"/> (serbest fikirden),
/// <see cref="Revision"/> (başka bir konseptin düzeltilmiş kopyası).
/// </summary>
public enum ConceptOrigin
{
    /// <summary>Konu önerisinden (başlık seçilerek).</summary>
    Topic = 0,

    /// <summary>Serbest fikirden.</summary>
    Idea = 1,

    /// <summary>Başka bir konseptin düzeltilmiş sürümü.</summary>
    Revision = 2,
}
