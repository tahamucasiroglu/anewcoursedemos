using Konsept.ArchitectureTests.Support;

namespace Konsept.ArchitectureTests.Rules;

/// <summary>
/// Kural kimliği → (ürün kümesinde çalıştır, fixture kümesinde çalıştır, fixture'da yakalanması
/// beklenen tip) eşlemesi. Aynı kural kodu iki kümede de koşar; böylece "kural ihlali yakalar"
/// kanıtı kalıcı ve tekrarlanabilirdir (plan 0001, R-4).
/// </summary>
public static class RuleCatalog
{
    /// <summary>Gets kural kimlikleri, testlerde veri kaynağı olarak kullanılır.</summary>
    public static TheoryData<string> Ids => [.. Rules.Keys.Order(StringComparer.Ordinal)];

    private static Dictionary<string, (Func<bool, IReadOnlyList<string>> Run, string ExpectedFixtureType)> Rules { get; } = new()
    {
        ["Y-1"] = (f => DependencyRules.Y1ModulesTalkOnlyThroughContracts(Arch(f)), "Konsept.Modules.Alpha.Application.CrossModuleUser"),
        ["Y-2"] = (f => DependencyRules.Y2DomainIsPure(Arch(f)), "Konsept.Modules.Alpha.Domain.ImpureEntity"),
        ["Y-3"] = (f => DependencyRules.Y3ApplicationDoesNotUseInfrastructure(Arch(f)), "Konsept.Modules.Alpha.Application.InfraUser"),
        ["Y-4"] = (f => DependencyRules.Y4NoForeignDbContext(Arch(f)), "Konsept.Modules.Alpha.Infrastructure.ForeignContextUser"),
        ["Y-5"] = (f => DependencyRules.Y5WebUsesOnlyApiClient(Arch(f)), "Konsept.Web.Controllers.LeakyWebController"),
        ["Y-6"] = (f => DependencyRules.Y6ControllersUseOnlyServices(Arch(f)), "Konsept.Modules.Alpha.Presentation.RepositoryController"),
        ["K-1"] = (f => StructureRules.K1NoNestedTypes(Assemblies(f)), "Konsept.Modules.Alpha.Domain.Outer"),
        ["K-2"] = (f => StructureRules.K2AbstractBaseConcrete(Assemblies(f)), "Konsept.Modules.Alpha.Application.Concrete.NakedService"),
        ["K-4"] = (f => StructureRules.K4ThinControllers(Assemblies(f)), "Konsept.Modules.Alpha.Presentation.FatController"),
    };

    /// <summary>Kuralı ürün derlemelerinde çalıştırır.</summary>
    /// <param name="id">Kural kimliği.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> RunOnProduct(string id) => Rules[id].Run(false);

    /// <summary>Kuralı fixture derlemesinde çalıştırır.</summary>
    /// <param name="id">Kural kimliği.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> RunOnFixture(string id) => Rules[id].Run(true);

    /// <summary>Fixture'da bu kuralın yakalaması gereken tipin tam adı.</summary>
    /// <param name="id">Kural kimliği.</param>
    /// <returns>Tip adı.</returns>
    public static string ExpectedFixtureType(string id) => Rules[id].ExpectedFixtureType;

    private static ArchUnitNET.Domain.Architecture Arch(bool fixture) =>
        fixture ? ArchitectureSources.Fixture : ArchitectureSources.Product;

    private static IReadOnlyList<System.Reflection.Assembly> Assemblies(bool fixture) =>
        fixture ? ArchitectureSources.FixtureAssemblies : ArchitectureSources.ProductAssemblies;
}
