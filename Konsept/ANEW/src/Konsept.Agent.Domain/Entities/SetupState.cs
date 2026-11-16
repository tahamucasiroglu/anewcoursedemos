namespace Konsept.Agent.Domain.Entities;

/// <summary>Kalıcı kurulum durumu (<c>setup-state.json</c>): kurulu bileşenler ve video klasörü (spec 0009 Req 9, 15, 19).</summary>
/// <param name="Installed">Bileşen kimliği → kayıt.</param>
/// <param name="VideoFolder">Seçilen video klasörü; seçilmediyse <c>null</c>.</param>
public sealed record SetupState(IReadOnlyDictionary<string, InstalledComponent> Installed, string? VideoFolder);
