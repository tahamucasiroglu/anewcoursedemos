namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Açılan Yeniden üret bağlamı (spec 0020 Req 4): Konseptler sayfası düzenlemeyi bu belirteçle açar.</summary>
/// <param name="Token">Belirteç.</param>
/// <param name="ConceptId">Konsept (video).</param>
/// <param name="ProfileId">Profil.</param>
/// <param name="BaseVersionId">Taban sürüm.</param>
public sealed record RegenerationContextDto(string Token, Guid ConceptId, Guid ProfileId, Guid BaseVersionId);
