namespace OkulYonetim.Application.Features.Administration;

/// <summary>Sayfalı kişi listesi: satırlar, toplam kişi sayısı, sayfa numarası ve sayfa büyüklüğü.</summary>
public sealed record MemberPage(IReadOnlyList<MemberRow> Items, int Total, int Page, int PageSize);
