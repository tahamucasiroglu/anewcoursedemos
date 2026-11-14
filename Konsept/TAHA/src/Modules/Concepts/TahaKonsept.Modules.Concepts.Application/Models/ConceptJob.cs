using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Kuyruktaki konsept üretim işi: kullanıcı, profil (kuyruğa alınırken aktif olan), tür, başlıklar ya da fikir + adet.
/// </summary>
public sealed record ConceptJob
{
    /// <summary>İş kimliği.</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Tür.</summary>
    public ConceptJobKind Kind { get; init; }

    /// <summary>Başlıklar (<see cref="ConceptJobKind.Topics"/>).</summary>
    public IReadOnlyList<string> Titles { get; init; } = [];

    /// <summary>Fikir (<see cref="ConceptJobKind.Idea"/>).</summary>
    public string? Idea { get; init; }

    /// <summary>Üretilecek adet.</summary>
    public int Count { get; init; }

    /// <summary>Kuyruğa alınma.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}
