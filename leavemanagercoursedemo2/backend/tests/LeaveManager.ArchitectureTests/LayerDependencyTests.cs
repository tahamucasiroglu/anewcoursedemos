using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using LeaveManager.Application.Health;
using LeaveManager.Domain.Results;
using LeaveManager.Infrastructure.Versioning;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace LeaveManager.ArchitectureTests;

/// <summary>Forbidden dependencies 1–4 (docs/architecture.md). Rule 5 (frontend) is enforced by ESLint.</summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(GetSystemHealth).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AssemblyVersionProvider).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain");
    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application");
    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure");
    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("Api");

    private static readonly string[] FrameworkPrefixes = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore"];

    [Fact]
    public void Rule1_Domain_DependsOnNoOtherLayer()
    {
        AssertRule(Types().That().Are(DomainLayer).Should()
            .NotDependOnAny(ApplicationLayer).AndShould()
            .NotDependOnAny(InfrastructureLayer).AndShould()
            .NotDependOnAny(ApiLayer));
    }

    [Fact]
    public void Rule1_Domain_ReferencesNoFrameworkPackage()
    {
        AssertNoReferenceTo(DomainAssembly, FrameworkPrefixes);
    }

    [Fact]
    public void Rule2_Application_DependsOnlyOnDomain()
    {
        AssertRule(Types().That().Are(ApplicationLayer).Should()
            .NotDependOnAny(InfrastructureLayer).AndShould()
            .NotDependOnAny(ApiLayer));
    }

    [Fact]
    public void Rule2_Application_ReferencesNoFrameworkPackage()
    {
        AssertNoReferenceTo(ApplicationAssembly, FrameworkPrefixes);
    }

    [Fact]
    public void Rule3_Infrastructure_DoesNotDependOnApi()
    {
        AssertRule(Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(ApiLayer));
        AssertNoReferenceTo(InfrastructureAssembly, ["LeaveManager.Api"]);
    }

    [Fact]
    public void Rule4_Api_UsesInfrastructureOnlyForCompositionRoot()
    {
        // Only the composition root (top-level Program) may touch Infrastructure — for DI setup.
        // The skeleton has no other Api type yet, so an empty match is allowed; the rule binds every future one.
        AssertRule(Types().That().Are(ApiLayer).And().DoNotHaveName("Program").Should()
            .NotDependOnAny(InfrastructureLayer)
            .WithoutRequiringPositiveResults());
    }

    [Fact]
    public void Rule4_Api_DoesNotUseEntityFrameworkDirectly()
    {
        AssertNoReferenceTo(ApiAssembly, ["Microsoft.EntityFrameworkCore"]);
    }

    private static void AssertRule(IArchRule rule)
    {
        var violations = rule.Evaluate(Architecture).Where(r => !r.Passed).Select(r => r.Description).ToList();
        Assert.True(violations.Count == 0, $"{rule.Description}:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static void AssertNoReferenceTo(Assembly assembly, string[] forbiddenPrefixes)
    {
        var offending = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToList();
        Assert.True(offending.Count == 0, $"{assembly.GetName().Name} references forbidden assemblies: {string.Join(", ", offending)}");
    }
}
