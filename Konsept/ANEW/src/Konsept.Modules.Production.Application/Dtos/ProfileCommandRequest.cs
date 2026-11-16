namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Profil düzeyindeki komut (Sırayla üret, Durdur).</summary>
public sealed record ProfileCommandRequest
{
    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }
}
