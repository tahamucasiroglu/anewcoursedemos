namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>
/// Admin alanının özeti (<c>GET api/admin/overview</c>). Bu spec'te iskelettir: bölüm listesi boştur; içeriğini 0010
/// (istatistikler, aktiviteler), 0024 (kullanıcılar, ban) ve 0025 (hata bildirimleri) doldurur. Kullanıcı verisi içermez
/// (BR-1).
/// </summary>
/// <param name="Sections">Admin bölümleri (şimdilik boş).</param>
public sealed record AdminOverviewDto(IReadOnlyList<string> Sections);
