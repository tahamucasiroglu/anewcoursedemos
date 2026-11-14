namespace TahaKonsept.Agent.EngineList.Abstract;

/// <summary>DI'a kayıtlı tüm yerel motorları (video, ses, görsel) grup bilgisiyle tek listede verir.</summary>
public interface IEngineCatalog
{
    /// <summary>Motorlar (grup, sonra ad sırasıyla; aynı anahtar bir kez).</summary>
    /// <returns>Liste.</returns>
    IReadOnlyList<EngineDescriptor> GetEngines();
}
