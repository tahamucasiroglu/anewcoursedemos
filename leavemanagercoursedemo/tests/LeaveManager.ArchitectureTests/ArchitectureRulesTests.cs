using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using LeaveManager.Application;
using LeaveManager.Domain;
using LeaveManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Razor;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;
using Type = System.Type;

namespace LeaveManager.ArchitectureTests;

/// <summary>docs/architecture.md, "Forbidden dependencies" 1–5.</summary>
public sealed class ArchitectureRulesTests
{
    private static readonly Assembly _domainAssembly = typeof(DomainAssemblyMarker).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(LeaveManagerDbContext).Assembly;
    private static readonly Assembly _webAssembly = typeof(Program).Assembly;

    // System.Private.CoreLib is loaded too so that the clock members of rule 5 exist in the model.
    private static readonly Architecture _architecture = new ArchLoader()
        .LoadAssemblies(_domainAssembly, _applicationAssembly, _infrastructureAssembly, _webAssembly, typeof(DateTime).Assembly)
        .Build();

    // Framework dependencies are checked on the compiled assembly references: ArchUnitNET only
    // sees types of loaded assemblies, and the compiler emits a reference only for what is used.
    private static readonly string[] _frameworkAssemblyPrefixes =
        ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Extensions.Identity"];

    // Rule 1
    [Fact]
    public void Rule1_DomainDependsOnNothing()
    {
        AssertNoReferenceTo(_domainAssembly,
            ["LeaveManager.Application", "LeaveManager.Infrastructure", "LeaveManager.Web", .. _frameworkAssemblyPrefixes]);

        Types().That().ResideInAssembly(_domainAssembly)
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(@"^LeaveManager\.(Application|Infrastructure|Web)(\..*)?$"))
            .Check(_architecture);
    }

    // Rule 2
    [Fact]
    public void Rule2_ApplicationDependsOnlyOnDomain()
    {
        AssertNoReferenceTo(_applicationAssembly,
            ["LeaveManager.Infrastructure", "LeaveManager.Web", .. _frameworkAssemblyPrefixes]);

        Types().That().ResideInAssembly(_applicationAssembly)
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(@"^LeaveManager\.(Infrastructure|Web)(\..*)?$"))
            .Check(_architecture);
    }

    // Rule 3
    [Fact]
    public void Rule3_WebUsesNoDbContextAndInfrastructureOnlyForRegistration()
    {
        AssertNoReferenceTo(_webAssembly, ["Microsoft.EntityFrameworkCore"]);

        Types().That().ResideInAssembly(_webAssembly)
            .And().DoNotHaveFullName(typeof(Program).FullName!)
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(@"^LeaveManager\.Infrastructure(\..*)?$"))
            .Check(_architecture);
    }

    // Rule 4
    [Fact]
    public void Rule4_ViewsTakeOnlyWebViewModels()
    {
        var views = _webAssembly.GetTypes()
            .Select(type => (Type: type, Model: RazorModelType(type)))
            .Where(view => view.Model is not null)
            .ToList();

        Assert.NotEmpty(views);
        Assert.All(views, view => Assert.True(
            view.Model == typeof(object)
                || (view.Model!.Namespace?.StartsWith("LeaveManager.Web", StringComparison.Ordinal) ?? false),
            $"{view.Type.FullName} uses model {view.Model}; views take only LeaveManager.Web view models."));
    }

    // Rule 5
    [Fact]
    public void Rule5_NoDirectSystemClock()
    {
        var rule = Types().That()
            .ResideInAssembly(_domainAssembly)
            .Or().ResideInAssembly(_applicationAssembly)
            .Or().ResideInAssembly(_infrastructureAssembly)
            .Or().ResideInAssembly(_webAssembly)
            .Should().NotCallAny(MethodMembers().That()
                .HaveFullNameMatching(@"^System\.(DateTime|DateTimeOffset) System\.(DateTime|DateTimeOffset)::get_(Now|UtcNow|Today)\(\)$"));

        rule.Check(_architecture);
    }

    private static void AssertNoReferenceTo(Assembly assembly, string[] forbiddenPrefixes)
    {
        var forbidden = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(prefix =>
                name.Equals(prefix, StringComparison.Ordinal)
                || name.StartsWith(prefix + ".", StringComparison.Ordinal)))
            .ToList();

        Assert.True(forbidden.Count == 0,
            $"{assembly.GetName().Name} must not reference: {string.Join(", ", forbidden)}");
    }

    private static Type? RazorModelType(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(RazorPage<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
