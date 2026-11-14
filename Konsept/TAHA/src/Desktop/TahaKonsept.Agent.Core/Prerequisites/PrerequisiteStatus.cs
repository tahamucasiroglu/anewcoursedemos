namespace TahaKonsept.Agent.Core.Prerequisites;

/// <summary>Kurulum ekranındaki bir bileşen satırı.</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="DisplayName">Ad.</param>
/// <param name="Description">Açıklama.</param>
/// <param name="Group">Grup.</param>
/// <param name="IsOptional">İsteğe bağlı mı.</param>
/// <param name="IsInstalled">Kurulu mu.</param>
/// <param name="InstalledVersion">Kurulu sürüm.</param>
public sealed record PrerequisiteStatus(
    string Id,
    string DisplayName,
    string Description,
    string Group,
    bool IsOptional,
    bool IsInstalled,
    string? InstalledVersion);
