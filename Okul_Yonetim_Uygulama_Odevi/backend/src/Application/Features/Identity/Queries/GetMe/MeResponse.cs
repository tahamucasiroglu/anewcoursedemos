using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// "Ben kimim" yanıtı: ad-soyad, e-posta, seçili okul (seçilmediyse boş), o okuldaki roller (Founder, Teacher, Guardian,
/// Accounting), görebildiği şubeler ve rolü olan tüm okullar (okul değiştirme için).
/// </summary>
public sealed record MeResponse(
    string FullName,
    string Email,
    TenantSummary? Tenant,
    IReadOnlyList<string> Roles,
    IReadOnlyList<BranchSummary> Branches,
    IReadOnlyList<TenantSummary> Tenants);
