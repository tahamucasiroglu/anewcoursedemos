namespace Konsept.Agent.Domain.Entities;

/// <summary>Kurulu bir bileşenin kaydı (spec 0009 Req 19–20): sürüm ve yürütülebilir dosyanın kurulumdaki hash'i.</summary>
/// <param name="Version">Kurulu sürüm.</param>
/// <param name="ExecutableSha256">Yürütülebilir dosyanın SHA-256'sı.</param>
public sealed record InstalledComponent(string Version, string ExecutableSha256);
