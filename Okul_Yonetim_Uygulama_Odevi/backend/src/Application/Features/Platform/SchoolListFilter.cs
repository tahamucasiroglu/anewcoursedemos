namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okul listesi süzgeci: ad araması (Türkçe harf kuralı), durum, sayfa.</summary>
public sealed record SchoolListFilter(string? Search, bool? Active, int Page, int PageSize);
