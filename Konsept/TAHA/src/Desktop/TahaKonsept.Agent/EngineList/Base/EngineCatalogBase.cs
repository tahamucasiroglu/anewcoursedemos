using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.EngineList.Abstract;

namespace TahaKonsept.Agent.EngineList.Base;

/// <summary>
/// <see cref="IEngineCatalog"/> tabanı: türetilen sınıfın verdiği (grup, motor) çiftlerini anahtara göre tekilleştirir,
/// gruba ve ada göre sıralar; sonuç ilk çağrıda hesaplanıp saklanır.
/// </summary>
public abstract class EngineCatalogBase : IEngineCatalog
{
    private IReadOnlyList<EngineDescriptor>? _engines;

    /// <inheritdoc />
    public IReadOnlyList<EngineDescriptor> GetEngines() => _engines ??= Build();

    /// <summary>Gruplu ham motor listesi.</summary>
    /// <returns>Çiftler.</returns>
    protected abstract IEnumerable<(EngineCategory Category, IEnumerable<IAgentEngine> Engines)> GetGroups();

    private List<EngineDescriptor> Build()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<EngineDescriptor>();
        foreach (var (category, engines) in GetGroups())
        {
            foreach (var engine in engines.OrderBy(e => e.DisplayName, StringComparer.CurrentCulture))
            {
                if (seen.Add(engine.Key))
                {
                    list.Add(new EngineDescriptor(engine, category));
                }
            }
        }

        return list;
    }
}
