using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace Konsept.ArchitectureTests.Support;

/// <summary>
/// Kuralların çalıştığı iki derleme kümesini yükler: gerçek ürün derlemeleri (<c>src/</c>) ve
/// kasıtlı ihlaller içeren fixture derlemesi. İkisi de bir kez yüklenip önbelleğe alınır.
/// </summary>
public static class ArchitectureSources
{
    /// <summary>Kasıtlı ihlalleri taşıyan derlemenin adı; gerçek kümeye asla girmez.</summary>
    public const string FixtureAssemblyName = "Konsept.ArchitectureTests.Fixtures";

    private static readonly Lazy<IReadOnlyList<Assembly>> ProductAssembliesLazy = new(LoadProductAssemblies);
    private static readonly Lazy<Architecture> ProductLazy = new(() => Build(ProductAssembliesLazy.Value));
    private static readonly Lazy<IReadOnlyList<Assembly>> FixtureAssembliesLazy =
        new(() => [Assembly.Load(FixtureAssemblyName)]);

    private static readonly Lazy<Architecture> FixtureLazy = new(() => Build(FixtureAssembliesLazy.Value));

    /// <summary>Gets ürün derlemeleri (<c>Konsept.*</c>; testler ve fixture hariç).</summary>
    public static IReadOnlyList<Assembly> ProductAssemblies => ProductAssembliesLazy.Value;

    /// <summary>Gets ürün derlemelerinin ArchUnitNET modeli.</summary>
    public static Architecture Product => ProductLazy.Value;

    /// <summary>Gets fixture derlemesi.</summary>
    public static IReadOnlyList<Assembly> FixtureAssemblies => FixtureAssembliesLazy.Value;

    /// <summary>Gets fixture derlemesinin ArchUnitNET modeli.</summary>
    public static Architecture Fixture => FixtureLazy.Value;

    private static Architecture Build(IReadOnlyList<Assembly> assemblies) =>
        new ArchLoader().LoadAssemblies([.. assemblies]).Build();

    private static List<Assembly> LoadProductAssemblies()
    {
        var names = Directory.GetFiles(AppContext.BaseDirectory, "Konsept.*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(IsProductAssembly)
            .Order(StringComparer.Ordinal);
        return [.. names.Select(Assembly.Load)];
    }

    private static bool IsProductAssembly(string name) =>
        !name.EndsWith(".Tests", StringComparison.Ordinal)
        && !name.StartsWith("Konsept.ArchitectureTests", StringComparison.Ordinal)
        && !name.Equals("Konsept.Testing", StringComparison.Ordinal);
}
