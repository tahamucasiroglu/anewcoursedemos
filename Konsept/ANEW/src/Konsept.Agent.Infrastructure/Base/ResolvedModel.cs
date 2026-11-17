using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>Çalıştırılmaya hazır yerel model (spec 0028): bileşen, aracın yürütülebilir dosyası ve klasör/betik yer tutucu değerleri.</summary>
/// <param name="Component">Model bileşeni.</param>
/// <param name="Executable">Aracın yürütülebilir dosyası.</param>
/// <param name="Folders">Yer tutucu → klasör/betik yolu.</param>
internal sealed record ResolvedModel(SetupComponent Component, string Executable, IReadOnlyDictionary<string, string> Folders)
{
    /// <summary>Klasör değerleriyle çalışma değerlerini birleştirir.</summary>
    /// <param name="runtime">Çalışma değerleri.</param>
    /// <returns>Tüm değerler.</returns>
    public IReadOnlyDictionary<string, string> Values(IReadOnlyDictionary<string, string> runtime)
    {
        var all = new Dictionary<string, string>(Folders, StringComparer.Ordinal);
        foreach (var (key, value) in runtime)
        {
            all[key] = value;
        }

        return all;
    }
}
