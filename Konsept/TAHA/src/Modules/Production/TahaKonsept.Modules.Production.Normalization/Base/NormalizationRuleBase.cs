using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Models;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Normalizasyon kurallarının ortak tabanı. <see cref="Apply"/> boş metni atlar ve <see cref="ApplyCore"/>'u çağırır.
/// <see cref="ReplaceMatches"/>: regex eşleşmelerini değerlendiricinin döndürdüğü okunuşla değiştirir ve her değişikliği
/// bu kuralın <see cref="Key"/> anahtarıyla kaydeder (değerlendirici <c>null</c> dönerse parça aynen kalır).
/// <see cref="SpaceBefore"/>: rakam/sözcüğe bitişik birim/sembol açılımlarında araya boşluk koyar.
/// </summary>
public abstract class NormalizationRuleBase : INormalizationRule
{
    /// <summary>Apostrof karakterleri (düz ve tipografik).</summary>
    protected const string Apostrophes = "'’";

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public void Apply(NormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Text.Length == 0)
        {
            return;
        }

        ApplyCore(context);
    }

    /// <summary>Kuralın asıl işi.</summary>
    /// <param name="context">Bağlam.</param>
    protected abstract void ApplyCore(NormalizationContext context);

    /// <summary>Regex eşleşmelerini değiştirir ve değişiklikleri kaydeder.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="regex">Desen.</param>
    /// <param name="evaluator">Eşleşme + girdi metni → okunuş (<c>null</c> = değiştirme).</param>
    protected void ReplaceMatches(NormalizationContext context, Regex regex, Func<Match, string, string?> evaluator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(regex);
        ArgumentNullException.ThrowIfNull(evaluator);
        var input = context.Text;
        context.Text = regex.Replace(input, m =>
        {
            var replacement = evaluator(m, input);
            if (replacement is null || string.Equals(replacement, m.Value, StringComparison.Ordinal))
            {
                return m.Value;
            }

            context.AddChange(m.Value, replacement, Key);
            return replacement;
        });
    }

    /// <summary>Eşleşmeden önceki karakter boşluk değilse (ve metnin başı değilse) başa boşluk ekler.</summary>
    /// <param name="input">Girdi metni.</param>
    /// <param name="index">Eşleşme başlangıcı.</param>
    /// <param name="replacement">Okunuş.</param>
    /// <returns>Gerekirse boşlukla başlayan okunuş.</returns>
    protected static string SpaceBefore(string input, int index, string replacement)
    {
        ArgumentNullException.ThrowIfNull(input);
        return index > 0 && !char.IsWhiteSpace(input[index - 1]) ? " " + replacement : replacement;
    }

    /// <summary>Eşleşmeden sonraki karakter harf/rakam ise sona boşluk ekler.</summary>
    /// <param name="input">Girdi metni.</param>
    /// <param name="end">Eşleşme sonu (hariç).</param>
    /// <param name="replacement">Okunuş.</param>
    /// <returns>Gerekirse boşlukla biten okunuş.</returns>
    protected static string SpaceAfter(string input, int end, string replacement)
    {
        ArgumentNullException.ThrowIfNull(input);
        return end < input.Length && char.IsLetterOrDigit(input[end]) ? replacement + " " : replacement;
    }

    /// <summary>Eşleşmenin ardından cümle bitiyor mu (metin sonu, satır sonu ya da boşluk + büyük harf).</summary>
    /// <param name="input">Girdi metni.</param>
    /// <param name="end">Eşleşme sonu (hariç).</param>
    /// <returns>Cümle sonuysa <c>true</c>.</returns>
    protected static bool IsSentenceEnd(string input, int end)
    {
        ArgumentNullException.ThrowIfNull(input);
        var i = end;
        if (i >= input.Length)
        {
            return true;
        }

        if (!char.IsWhiteSpace(input[i]))
        {
            return false;
        }

        while (i < input.Length && char.IsWhiteSpace(input[i]))
        {
            if (input[i] == '\n')
            {
                return true;
            }

            i++;
        }

        return i >= input.Length || char.IsUpper(input[i]);
    }

    /// <summary>Eşleşmeden sonraki ilk sözcük büyük harfle mi başlıyor.</summary>
    /// <param name="input">Girdi metni.</param>
    /// <param name="end">Eşleşme sonu (hariç).</param>
    /// <returns>Büyük harfle başlıyorsa <c>true</c>.</returns>
    protected static bool NextWordIsCapitalized(string input, int end)
    {
        ArgumentNullException.ThrowIfNull(input);
        var i = end;
        while (i < input.Length && char.IsWhiteSpace(input[i]))
        {
            i++;
        }

        return i < input.Length && char.IsUpper(input[i]);
    }
}
