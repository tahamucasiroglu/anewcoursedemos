namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>"Üretim sırasına al" (spec 0006 Req 10): profil ve listedeki görünüş sırasıyla konseptler.</summary>
public sealed record EnqueueRequest
{
    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets konseptler (görünüş sırasıyla).</summary>
    public IReadOnlyList<Guid> ConceptIds { get; init; } = [];
}
