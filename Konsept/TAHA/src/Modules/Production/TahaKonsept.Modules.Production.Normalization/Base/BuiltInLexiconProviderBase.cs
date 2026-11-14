using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Yerleşik sözlük sağlayıcılarının ortak tabanı: girdileri bir kez yükler (<see cref="Load"/>), boş/yinelenen yazılışları
/// eler (önce gelen kazanır) ve önbellekte tutar. Önemli üye: <see cref="Entries"/>.
/// </summary>
public abstract class BuiltInLexiconProviderBase : IBuiltInLexiconProvider
{
    private readonly Lazy<IReadOnlyList<PronunciationEntry>> _entries;

    /// <summary>Tembel yükleyiciyi hazırlar.</summary>
    protected BuiltInLexiconProviderBase()
    {
        _entries = new Lazy<IReadOnlyList<PronunciationEntry>>(LoadAndClean, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IReadOnlyList<PronunciationEntry> Entries => _entries.Value;

    /// <summary>Ham girdileri yükler.</summary>
    /// <returns>Ham girdiler.</returns>
    protected abstract IEnumerable<PronunciationEntry> Load();

    private IReadOnlyList<PronunciationEntry> LoadAndClean()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Load()
            .Where(e => !string.IsNullOrWhiteSpace(e.Written) && !string.IsNullOrWhiteSpace(e.Spoken))
            .Where(e => seen.Add(e.Written.Trim()))
            .Select(e => new PronunciationEntry { Written = e.Written.Trim(), Spoken = e.Spoken.Trim() })
            .ToList();
    }
}
