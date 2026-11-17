namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Yeni sürümde seçilebilen bir model (spec 0020 Req 2).</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Local">Yerel (ücretsiz) ise <c>true</c>, online ise <c>false</c>.</param>
public sealed record VersionOptionDto(string Id, string Name, bool Local);
