using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// "Ben kimim" yanıtı: ad-soyad, e-posta, seçili okul (seçilmediyse boş), o okuldaki roller (Founder, Teacher, Guardian,
/// Accounting), görebildiği şubeler, rolü olan tüm okullar (okul değiştirme için) ve platform paneline erişim (spec 0007:
/// platform yöneticisi ve kodla açılmış oturum), velinin okunmamış bildirim sayısı (spec 0010; veli değilse 0) ve okunmamış
/// duyuru (veli) ile mesaj (öğretmen/veli) sayıları (spec 0011).
/// </summary>
public sealed record MeResponse(
    string FullName,
    string Email,
    TenantSummary? Tenant,
    IReadOnlyList<string> Roles,
    IReadOnlyList<BranchSummary> Branches,
    IReadOnlyList<TenantSummary> Tenants,
    bool IsPlatformAdmin = false,
    int UnreadNoticeCount = 0,
    int UnreadAnnouncementCount = 0,
    int UnreadMessageCount = 0);
