namespace Konsept.Modules.Concepts.Contracts.Dtos;

/// <summary>Silinenler'deki bir videonun konsept başlığı ve silinme anı (spec 0019 Req 6).</summary>
/// <param name="Title">Başlık.</param>
/// <param name="DeletedAt">Silinme anı (UTC).</param>
public sealed record DislikedVideoTitle(string Title, DateTimeOffset DeletedAt);
