namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konsept üretim işinin türü: seçilen başlıklardan ya da serbest fikirden.
/// </summary>
public enum ConceptJobKind
{
    /// <summary>Seçilen başlıklardan.</summary>
    Topics = 0,

    /// <summary>Serbest fikirden.</summary>
    Idea = 1,
}
