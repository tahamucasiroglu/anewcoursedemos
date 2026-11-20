namespace OkulYonetim.Application.Features.Students;

/// <summary>Öğrenci listesinin bir sayfası (25'lik): satırlar, süzgeçten sonraki toplam, sayfa numarası ve boyutu.</summary>
public sealed record StudentPage(IReadOnlyList<StudentRow> Items, int Total, int Page, int PageSize);
