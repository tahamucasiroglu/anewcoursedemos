namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>İşe sabitlenen değerler (spec 0006 Req 7, 20; AC-7, AC-25).</summary>
/// <param name="Settings">Ayarlar (altyazı ses kapalıysa <c>false</c>).</param>
/// <param name="Language">Konseptin kaydındaki dil.</param>
/// <param name="Format">Konseptin kaydındaki biçim.</param>
/// <param name="TargetDurationSeconds">Konseptin kaydındaki hedef süre.</param>
public sealed record FixedSettingsDto(ProductionSettingsDto Settings, string Language, string Format, int TargetDurationSeconds);
