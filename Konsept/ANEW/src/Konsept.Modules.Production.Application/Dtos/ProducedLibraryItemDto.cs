namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Üretilen Videolar listesindeki video (spec 0007 Req 1): kimlik konsept kimliğidir; başlık ve süre en son sürümden.</summary>
/// <param name="Id">Video (konsept).</param>
/// <param name="Title">Başlık.</param>
/// <param name="DurationSeconds">Video süresi (sn).</param>
/// <param name="ProducedAt">Üretim tarihi: ilk tamamlanan sürümün bitişi (UTC).</param>
/// <param name="VersionCount">Sürüm sayısı.</param>
/// <param name="Status">Üretimi süren/hata veren sürümün durumu (spec 0020 Req 9 rozeti) ya da <c>null</c>.</param>
/// <param name="Stage">O anki aşama ya da <c>null</c>.</param>
public sealed record ProducedLibraryItemDto(
    Guid Id, string Title, int DurationSeconds, DateTimeOffset ProducedAt, int VersionCount, string? Status = null, string? Stage = null);
