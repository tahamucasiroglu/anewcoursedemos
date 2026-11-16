namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>İş satırındaki bir aşama (spec 0017 Req 6, 14a).</summary>
/// <param name="Stage">Aşama: <c>analysis</c> · <c>code</c> · <c>images</c> · <c>voice</c> · <c>render</c> · <c>merge</c>.</param>
/// <param name="State">Durum: <c>pending</c> · <c>running</c> · <c>done</c> · <c>skipped</c> · <c>failed</c>.</param>
/// <param name="Device">Aygıt (<c>gpu</c> · <c>cpu</c>) ya da <c>null</c>.</param>
public sealed record StageDto(string Stage, string State, string? Device);
