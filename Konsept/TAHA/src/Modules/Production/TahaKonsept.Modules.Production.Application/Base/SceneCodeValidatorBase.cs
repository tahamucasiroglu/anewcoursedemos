using System.Text.RegularExpressions;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="ISceneCodeValidator"/> ortak davranışı: LLM metninden Markdown kod çitini ayıklar (varsa en uzun bloğu alır), boşluğu temizler,
/// ağ/dosya erişimi gibi açık yasakları reddeder ve motora özgü zorunlu işaretleri <see cref="RequiredMarkers"/> ile denetler.
/// </summary>
public abstract partial class SceneCodeValidatorBase : ISceneCodeValidator
{
    /// <summary>Temiz sahne kodu ya da doğrulama hatası.</summary>
    /// <param name="engineKey">Motor.</param>
    /// <param name="rawText">LLM metni.</param>
    /// <returns>Kod ya da hata.</returns>
    public Result<string> Extract(string engineKey, string rawText)
    {
        var code = StripFences(rawText ?? string.Empty).Trim();
        if (code.Length < 40)
        {
            return ProductionErrors.InvalidSceneCode("çıktı boş ya da çok kısa");
        }

        foreach (var banned in BannedTokens)
        {
            if (code.Contains(banned, StringComparison.OrdinalIgnoreCase))
            {
                return ProductionErrors.InvalidSceneCode("yasak ifade: " + banned);
            }
        }

        var markers = RequiredMarkers(engineKey?.Trim().ToLowerInvariant() ?? string.Empty);
        if (markers is null)
        {
            return ProductionErrors.UnknownEngine(engineKey ?? string.Empty);
        }

        var missing = markers.Where(m => !code.Contains(m, StringComparison.Ordinal)).ToList();
        return missing.Count == 0
            ? code
            : ProductionErrors.InvalidSceneCode("eksik zorunlu öğe: " + string.Join(", ", missing));
    }

    /// <summary>Motorun zorunlu işaretleri (bilinmeyen motor için <c>null</c>).</summary>
    /// <param name="engineKey">Motor (küçük harf).</param>
    /// <returns>İşaretler.</returns>
    protected abstract IReadOnlyList<string>? RequiredMarkers(string engineKey);

    /// <summary>Hiçbir motorda izin verilmeyen ifadeler.</summary>
    protected virtual IReadOnlyList<string> BannedTokens { get; } = ["file://", "XMLHttpRequest", "new WebSocket", "fetch(", "eval("];

    private static string StripFences(string text)
    {
        var blocks = FenceRegex().Matches(text).Select(m => m.Groups["code"].Value).ToList();
        return blocks.Count == 0 ? text : blocks.OrderByDescending(b => b.Length).First();
    }

    [GeneratedRegex("```[a-zA-Z0-9]*[ \\t]*\\r?\\n(?<code>.*?)```", RegexOptions.Singleline)]
    private static partial Regex FenceRegex();
}
