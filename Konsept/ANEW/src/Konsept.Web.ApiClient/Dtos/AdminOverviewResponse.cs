namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Admin alanının özeti (<c>GET api/admin/overview</c>); bu spec'te iskelet, bölüm listesi boş (spec 0011 Req 4).</summary>
/// <param name="Sections">Admin bölümleri.</param>
public sealed record AdminOverviewResponse(IReadOnlyList<string> Sections);
