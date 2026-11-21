namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>CSV hücresinin türü (spec 0017 C-5): metin (formül kaçışı ve tırnaklama), tutar ("1234,50"), tam sayı ve gün ("GG.AA.YYYY").</summary>
public enum CsvCellKind
{
    Text = 1,
    Money = 2,
    Number = 3,
    Day = 4,
}
