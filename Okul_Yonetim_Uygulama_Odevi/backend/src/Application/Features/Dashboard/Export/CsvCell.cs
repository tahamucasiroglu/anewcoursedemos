namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>
/// CSV dosyasının bir hücresi (spec 0017 C-5): türü (<see cref="CsvCellKind"/>) ve değeri. Yalnızca metin hücresi formül kaçışından
/// geçer; tutar, sayı ve gün hücreleri biçimlenir ama kaçışlanmaz (Excel onları sayı/tarih olarak okusun — plan 0017 Risks 2).
/// </summary>
public readonly record struct CsvCell(CsvCellKind Kind, string? Text, decimal Value, DateOnly Day)
{
    public static CsvCell Of(string? text) => new(CsvCellKind.Text, text, 0m, default);

    public static CsvCell Money(decimal amount) => new(CsvCellKind.Money, null, amount, default);

    public static CsvCell Number(long value) => new(CsvCellKind.Number, null, value, default);

    public static CsvCell Date(DateOnly day) => new(CsvCellKind.Day, null, 0m, day);

    /// <summary>Boş hücre (ör. toplam satırında karşılığı olmayan sütun).</summary>
    public static CsvCell Empty => Of(null);
}
