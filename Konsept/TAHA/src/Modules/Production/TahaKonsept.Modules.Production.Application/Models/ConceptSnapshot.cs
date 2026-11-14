using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>IConceptSource</c> sonucu: konseptin üretim anındaki hali (başlık, profil, tam konsept belgesi).
/// </summary>
public sealed record ConceptSnapshot
{
    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Profil (biliniyorsa; çözünürlük biçimi için).</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Konsept belgesi.</summary>
    public ConceptDocument Document { get; init; } = new();
}
