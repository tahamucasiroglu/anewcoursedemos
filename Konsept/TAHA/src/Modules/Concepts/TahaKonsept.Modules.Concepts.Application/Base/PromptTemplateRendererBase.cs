using System.Text.RegularExpressions;
using TahaKonsept.Modules.Concepts.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IPromptTemplateRenderer"/> ortak davranışı: önce koşullu bölümler (<c>{{#ad}}…{{/ad}}</c>; tek başına bir satırsa
/// boş kalınca satırıyla silinir), sonra değişkenler (<c>{{ad}}</c>; bilinmeyen = boş), en sonda 2'den fazla boş satır sıkıştırılır.
/// </summary>
public abstract partial class PromptTemplateRendererBase : IPromptTemplateRenderer
{
    /// <summary>Şablonu işler.</summary>
    /// <param name="template">Şablon.</param>
    /// <param name="values">Değerler.</param>
    /// <returns>Metin.</returns>
    public virtual string Render(string template, IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);
        var text = template.Replace("\r\n", "\n", StringComparison.Ordinal);
        text = LineSectionRegex().Replace(text, m => HasValue(values, m.Groups[1].Value) ? m.Groups[2].Value + "\n" : string.Empty);
        text = InlineSectionRegex().Replace(text, m => HasValue(values, m.Groups[1].Value) ? m.Groups[2].Value : string.Empty);
        text = VariableRegex().Replace(text, m => values.TryGetValue(m.Groups[1].Value, out var v) ? (v ?? string.Empty).Trim() : string.Empty);
        text = BlankLinesRegex().Replace(text, "\n\n");
        return text.Trim();
    }

    private static bool HasValue(IReadOnlyDictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);

    [GeneratedRegex(@"^[ \t]*\{\{#(\w+)\}\}(.*?)\{\{/\1\}\}[ \t]*\n", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex LineSectionRegex();

    [GeneratedRegex(@"\{\{#(\w+)\}\}(.*?)\{\{/\1\}\}", RegexOptions.Singleline)]
    private static partial Regex InlineSectionRegex();

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"\n[ \t]*\n(?:[ \t]*\n)+")]
    private static partial Regex BlankLinesRegex();
}
