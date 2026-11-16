namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Üretilenler listesindeki video (spec 0017 Req 20): başlık ve bitiş zamanı (UTC); dosya ajandadır.</summary>
/// <param name="Id">Video.</param>
/// <param name="Title">Başlık.</param>
/// <param name="EndedAt">Bitiş (UTC).</param>
public sealed record ProducedVideoDto(Guid Id, string Title, DateTimeOffset EndedAt);
