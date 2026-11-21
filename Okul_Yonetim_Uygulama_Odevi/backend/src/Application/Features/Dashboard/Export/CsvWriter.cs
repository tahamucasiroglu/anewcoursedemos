using System.Globalization;
using System.Text;

namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>
/// Türkçe Excel'in çift tıkla açtığı CSV dosyası (spec 0017 C-5, gereksinim 10; saf): UTF-8 BOM, ayırıcı noktalı virgül, satır sonu CRLF,
/// tutar ondalık virgüllü ve binlik ayırıcısız ("1234,50"), gün "GG.AA.YYYY". Metin hücresi <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, sekme,
/// satır başı ya da satır sonu ile başlıyorsa başına <c>'</c> eklenir (CSV/formül enjeksiyonu — <c>docs/security.md</c> Input &amp; output 3);
/// <c>;</c>, <c>,</c>, <c>"</c> ya da satır kırığı içeren metin çift tırnağa alınır (içindeki tırnak ikilenir) — virgül, liste ayırıcısı virgül
/// olan ortamda hücrenin bölünüp ikinci parçanın formül olarak okunmasını önler (review tur 1 R1-3). Sayı ve gün hücreleri kaçışlanmaz.
/// <c>sep=;</c> satırı yazılmaz (bazı Excel sürümlerinde BOM algısını bozar — plan 0017 Risks 1).
/// </summary>
public static class CsvWriter
{
    public const char Separator = ';';
    public const string LineEnd = "\r\n";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly char[] FormulaStarts = ['=', '+', '-', '@', '\t', '\r', '\n'];
    private static readonly char[] QuoteTriggers = [Separator, ',', '"', '\r', '\n'];

    public static byte[] Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<CsvCell>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        StringBuilder text = new();
        AppendLine(text, headers.Select(CsvCell.Of).ToList());
        foreach (IReadOnlyList<CsvCell> row in rows)
        {
            AppendLine(text, row);
        }

        byte[] preamble = Utf8WithBom.GetPreamble();
        byte[] body = Utf8WithBom.GetBytes(text.ToString());
        return [.. preamble, .. body];
    }

    public static string Field(CsvCell cell) => cell.Kind switch
    {
        CsvCellKind.Money => cell.Value.ToString("0.00", Turkish),
        CsvCellKind.Number => cell.Value.ToString("0", CultureInfo.InvariantCulture),
        CsvCellKind.Day => cell.Day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        _ => TextField(cell.Text),
    };

    private static string TextField(string? value)
    {
        string text = value ?? string.Empty;
        if (text.Length > 0 && Array.IndexOf(FormulaStarts, text[0]) >= 0)
        {
            text = "'" + text;
        }

        return text.IndexOfAny(QuoteTriggers) >= 0
            ? "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : text;
    }

    private static void AppendLine(StringBuilder text, IReadOnlyList<CsvCell> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                text.Append(Separator);
            }

            text.Append(Field(cells[i]));
        }

        text.Append(LineEnd);
    }
}
