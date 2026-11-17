namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Videonun üretimi süren ya da hata veren sürümü (spec 0020 Req 9–10): iş, tür (<c>new</c> · <c>regenerate</c>), durum (0006 Req 13 anahtarı),
/// o anki aşama, sade hata, aşamalar ve o ana kadarki online maliyet.
/// </summary>
/// <param name="JobId">İş.</param>
/// <param name="Kind">Tür.</param>
/// <param name="Status">Durum: <c>queued · producing · stopped · waiting_for_agent · failed</c>.</param>
/// <param name="Stage">Çalışan aşama ya da <c>null</c>.</param>
/// <param name="Error">Sade hata (yalnızca <c>failed</c>).</param>
/// <param name="Stages">Aşamalar.</param>
/// <param name="CostUsd">Online maliyet (USD).</param>
public sealed record ActiveVersionDto(
    Guid JobId, string Kind, string Status, string? Stage, string? Error, IReadOnlyList<StageDto> Stages, decimal CostUsd);
