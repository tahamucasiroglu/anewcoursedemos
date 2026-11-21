namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>Dışa aktarılan dosya (spec 0017 gereksinim 10): ad (<c>&lt;rapor&gt;-GG.AA.YYYY.csv</c>), içerik (UTF-8 BOM'lu CSV) ve veri satırı sayısı.</summary>
public sealed record ReportFile(string FileName, byte[] Content, int RowCount);
