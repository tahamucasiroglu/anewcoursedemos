using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Models;

/// <summary>
/// Günlük arama filtresi (<c>GET api/diagnostics/activity-logs?day=2026-11-14&amp;level=Error&amp;source=Production&amp;page=1&amp;pageSize=50</c>).
/// <see cref="Day"/> Türkiye saatine göre gündür. Yönetici tüm kullanıcıları, diğerleri yalnızca kendi kayıtlarını görür.
/// </summary>
public sealed record ActivityLogQuery
{
    /// <summary>Gün (Türkiye saati); <c>null</c> ise tüm günler.</summary>
    public DateOnly? Day { get; init; }

    /// <summary>Seviye; <c>null</c> ise hepsi.</summary>
    public LogLevelKind? Level { get; init; }

    /// <summary>Kaynak (tam eşleşme); boşsa hepsi.</summary>
    public string? Source { get; init; }

    /// <summary>Mesajda geçen metin; boşsa filtre yok.</summary>
    public string? Search { get; init; }

    /// <summary>Sayfa (1'den başlar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–200, varsayılan 50).</summary>
    public int PageSize { get; init; } = 50;
}
