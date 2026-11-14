using System.Text.Json;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// Yerleşik okunuş sözlüğünü derlemeye gömülü <c>Resources/builtin-lexicon.tr.json</c> kaynağından okur
/// (<c>[{"written":"Google","spoken":"Gugıl"}, …]</c>). Normalizasyondaki tek G/Ç budur; sonuç önbelleklenir.
/// </summary>
public sealed class EmbeddedBuiltInLexiconProvider : BuiltInLexiconProviderBase
{
    /// <summary>Gömülü kaynağın mantıksal adı.</summary>
    public const string ResourceName = "TahaKonsept.Normalization.builtin-lexicon.tr.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    protected override IEnumerable<PronunciationEntry> Load()
    {
        using var stream = typeof(EmbeddedBuiltInLexiconProvider).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Gömülü sözlük bulunamadı: {ResourceName}");
        return JsonSerializer.Deserialize<List<PronunciationEntry>>(stream, JsonOptions) ?? [];
    }
}
