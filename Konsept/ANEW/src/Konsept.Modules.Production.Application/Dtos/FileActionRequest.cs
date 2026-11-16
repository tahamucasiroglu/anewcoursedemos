namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Dosya eylemi isteği (spec 0007 Req 4): <c>folder</c> (Klasörde aç) ya da <c>player</c> (Sistem oynatıcısında aç).</summary>
/// <param name="Action">Eylem.</param>
public sealed record FileActionRequest(string? Action);
