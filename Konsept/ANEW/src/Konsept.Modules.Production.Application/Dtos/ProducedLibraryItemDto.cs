namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Üretilen Videolar listesindeki video (spec 0007 Req 1): kimlik konsept kimliğidir; başlık ve süre en son sürümden.</summary>
/// <param name="Id">Video (konsept).</param>
/// <param name="Title">Başlık.</param>
/// <param name="DurationSeconds">Video süresi (sn).</param>
/// <param name="ProducedAt">Üretim tarihi: ilk tamamlanan sürümün bitişi (UTC).</param>
/// <param name="VersionCount">Sürüm sayısı.</param>
public sealed record ProducedLibraryItemDto(Guid Id, string Title, int DurationSeconds, DateTimeOffset ProducedAt, int VersionCount);
