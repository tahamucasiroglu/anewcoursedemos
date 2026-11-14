using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Sitenin durum alanında gösterilen sistem durumu (spec 0001 Req 5, AC-4).
/// </summary>
/// <param name="Version">Tek ürün versiyonu.</param>
/// <param name="DatabaseAvailable">Veritabanına erişilebiliyor mu.</param>
/// <param name="Agent">Ajan durumu.</param>
/// <param name="LastEvent">Günlük şeridindeki son olay; yoksa ya da veritabanı erişilemezse <c>null</c>.</param>
public sealed record SystemStatusDto(string Version, bool DatabaseAvailable, AgentStatusDto Agent, SystemEventDto? LastEvent);
