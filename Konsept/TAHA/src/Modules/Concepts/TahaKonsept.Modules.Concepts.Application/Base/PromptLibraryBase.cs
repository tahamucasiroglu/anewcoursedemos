using System.Collections.Concurrent;
using TahaKonsept.Modules.Concepts.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IPromptLibrary"/> ortak davranışı: şablonu bir kez yükler (<see cref="Load"/>) ve bellekte tutar.
/// </summary>
public abstract class PromptLibraryBase : IPromptLibrary
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    /// <summary>Şablonu getirir.</summary>
    /// <param name="name">Ad.</param>
    /// <returns>Şablon.</returns>
    public string Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _cache.GetOrAdd(name, Load);
    }

    /// <summary>Şablonu kaynağından okur.</summary>
    /// <param name="name">Ad.</param>
    /// <returns>Şablon.</returns>
    protected abstract string Load(string name);
}
