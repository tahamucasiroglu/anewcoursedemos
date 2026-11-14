using Konsept.ArchitectureTests.Rules;
using Konsept.ArchitectureTests.Support;
using Shouldly;

namespace Konsept.ArchitectureTests;

/// <summary>
/// Y-1…Y-6, K-1, K-2 ve K-4 kurallarının ürün kodunda sağlandığını ve aynı kuralların kasıtlı
/// ihlalleri yakaladığını doğrular (spec 0001 AC-9).
/// </summary>
public sealed class ArchitectureRulesTests
{
    /// <summary>Ürün derlemelerinde kural ihlali yoktur.</summary>
    /// <param name="ruleId">Kural kimliği.</param>
    [Theory]
    [MemberData(nameof(RuleCatalog.Ids), MemberType = typeof(RuleCatalog))]
    public void ArchitectureRules_ProductAssemblies_HaveNoViolations(string ruleId)
    {
        var violations = RuleCatalog.RunOnProduct(ruleId);

        violations.ShouldBeEmpty($"{ruleId} ihlalleri:\n{string.Join('\n', violations)}");
    }

    /// <summary>Her kural, fixture derlemesindeki kendi kasıtlı ihlalini yakalar (kural kırmızıya dönebilir).</summary>
    /// <param name="ruleId">Kural kimliği.</param>
    [Theory]
    [MemberData(nameof(RuleCatalog.Ids), MemberType = typeof(RuleCatalog))]
    public void ArchitectureRules_FixtureViolations_AreDetected(string ruleId)
    {
        var violations = RuleCatalog.RunOnFixture(ruleId);

        violations.ShouldContain(v => v.StartsWith(RuleCatalog.ExpectedFixtureType(ruleId), StringComparison.Ordinal));
    }

    /// <summary>
    /// <c>src/</c> altındaki her proje mimari testlerine yüklenir; referansı unutulan proje
    /// kuralların dışında sessizce kalmaz.
    /// </summary>
    [Fact]
    public void ProductAssemblies_EverySourceProject_IsLoaded()
    {
        var expected = Directory.GetFiles(RepositoryPaths.SourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToList();
        var loaded = ArchitectureSources.ProductAssemblies.Select(a => a.GetName().Name).ToList();

        expected.Except(loaded).ShouldBeEmpty();
    }
}
