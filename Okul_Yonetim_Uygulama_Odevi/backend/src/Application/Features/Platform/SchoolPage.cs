namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okul listesinin bir sayfası.</summary>
public sealed record SchoolPage(IReadOnlyList<SchoolRow> Items, int Total, int Page, int PageSize);
