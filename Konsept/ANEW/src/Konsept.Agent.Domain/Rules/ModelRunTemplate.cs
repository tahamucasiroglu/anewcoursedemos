using System.Text.RegularExpressions;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Model çalıştırma şablonunun yer tutucuları (spec 0028 Req 7, 8): <c>{self}</c> modelin klasörü, <c>{&lt;bileşen&gt;}</c> bağımlılığın klasörü,
/// <c>{script:&lt;ad&gt;}</c> ajana gömülü çalıştırma betiği, çalışma değerleri (<see cref="Prompt"/>, <see cref="Output"/> …). Tek geçişte çözülür:
/// değerin içindeki süslü parantez yeniden yorumlanmaz; bilinmeyen yer tutucu hatadır (sessizce boş geçilmez).
/// </summary>
public static partial class ModelRunTemplate
{
    /// <summary>Modelin kendi klasörü.</summary>
    public const string Self = "self";

    /// <summary>Görsel tarifi.</summary>
    public const string Prompt = "prompt";

    /// <summary>Çıktı dosyası.</summary>
    public const string Output = "output";

    /// <summary>Girdi dosyası (ses: sahne listesi).</summary>
    public const string Input = "input";

    /// <summary>Genişlik.</summary>
    public const string Width = "width";

    /// <summary>Yükseklik.</summary>
    public const string Height = "height";

    /// <summary>Tohum.</summary>
    public const string Seed = "seed";

    /// <summary>Dil.</summary>
    public const string Language = "lang";

    /// <summary>Betik yer tutucusunun öneki.</summary>
    public const string ScriptPrefix = "script:";

    /// <summary>Çalışma anında verilen değerlerin adları.</summary>
    public static readonly IReadOnlyList<string> RuntimeNames = [Prompt, Output, Input, Width, Height, Seed, Language];

    /// <summary>Şablonu çözer.</summary>
    /// <param name="args">Argüman şablonu.</param>
    /// <param name="values">Ad → değer.</param>
    /// <returns>Argümanlar.</returns>
    /// <exception cref="InvalidOperationException">Bilinmeyen yer tutucu.</exception>
    public static IReadOnlyList<string> Expand(IReadOnlyList<string> args, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(values);
        return [.. args.Select(a => Token().Replace(a, m => values.TryGetValue(m.Groups[1].Value, out var v)
            ? v
            : throw new InvalidOperationException("Bilinmeyen yer tutucu: " + m.Groups[1].Value)))];
    }

    /// <summary>Şablondaki yer tutucu adları.</summary>
    /// <param name="args">Argüman şablonu.</param>
    /// <returns>Adlar.</returns>
    public static IReadOnlyList<string> Names(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return [.. args.SelectMany(a => Token().Matches(a).Select(m => m.Groups[1].Value)).Distinct(StringComparer.Ordinal)];
    }

    [GeneratedRegex(@"\{([a-z0-9][a-z0-9:._\-]*)\}")]
    private static partial Regex Token();
}
