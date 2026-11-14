using TahaKonsept.Modules.Production.Normalization.Base;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// Türkçe sayı sözcük tabloları (bir … dokuz, on … doksan, yüz, bin, milyon, milyar, trilyon, katrilyon, kentilyon).
/// Algoritma <see cref="NumberToTurkishWordsBase"/> içinde. Statik kısayol: <c>Text.NumberToTurkishWords</c>.
/// </summary>
public sealed class TurkishNumberToWords : NumberToTurkishWordsBase
{
    private static readonly string[] OnesTable = ["", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"];
    private static readonly string[] TensTable = ["", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan"];
    private static readonly string[] ScalesTable = ["", "bin", "milyon", "milyar", "trilyon", "katrilyon", "kentilyon"];

    /// <inheritdoc />
    protected override IReadOnlyList<string> Ones => OnesTable;

    /// <inheritdoc />
    protected override IReadOnlyList<string> Tens => TensTable;

    /// <inheritdoc />
    protected override IReadOnlyList<string> Scales => ScalesTable;

    /// <inheritdoc />
    protected override string Hundred => "yüz";

    /// <inheritdoc />
    protected override string Zero => "sıfır";

    /// <inheritdoc />
    protected override string Minus => "eksi";
}
