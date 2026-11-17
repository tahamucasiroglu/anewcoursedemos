namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Silinenler'in Videolar satırı (spec 0019 Req 3): video (konsept) kimliği, başlık, silinme zamanı, sürüm sayısı.</summary>
/// <param name="Id">Video (konsept) kimliği.</param>
/// <param name="Title">Başlık (en son sürümün).</param>
/// <param name="DeletedAt">Silinme zamanı (UTC).</param>
/// <param name="VersionCount">Sürüm sayısı.</param>
public sealed record DeletedVideoItemDto(Guid Id, string Title, DateTimeOffset DeletedAt, int VersionCount);
