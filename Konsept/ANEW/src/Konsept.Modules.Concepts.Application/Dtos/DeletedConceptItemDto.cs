namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Silinenler satırı (spec 0016 Req 4): başlık ve silinme zamanı.</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Title">Başlık.</param>
/// <param name="DeletedAt">Silinme zamanı (UTC).</param>
public sealed record DeletedConceptItemDto(Guid Id, string Title, DateTimeOffset DeletedAt);
