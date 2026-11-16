namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Yerel akış adresi (spec 0017 Req 21): yalnızca bu bilgisayardan açılan, kısa ömürlü yetkili adres.</summary>
/// <param name="Url">Adres.</param>
public sealed record VideoStreamDto(string Url);
