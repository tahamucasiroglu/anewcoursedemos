using System.Text.RegularExpressions;
using NetArchTest.Rules;
using TahaKonsept.ArchitectureTests.Support;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.BuildingBlocks.Infrastructure.Time.Concrete;
using TahaKonsept.Shared.Contracts.Agent;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (f) Katman bağımlılıkları. csproj referansları üzerinden (yeni modüller otomatik kapsanır):
/// Domain → Application/Infrastructure/Api/Host'a referans vermez; Application → Infrastructure/Api'ye vermez; Infrastructure → Api'ye vermez;
/// Shared.Contracts hiçbir projeye referans vermez; bir modül başka modülün Domain/Infrastructure/Api projesine referans vermez
/// (yalnızca Application/Contracts serbest); BuildingBlocks modüllere/host'lara bağlı değildir.
/// Derlenmiş BuildingBlocks derlemeleri NetArchTest ile ayrıca denetlenir.
/// </summary>
public sealed partial class LayerDependencyTests
{
    private static readonly IReadOnlyList<ProjectFile> Projects = ProjectFile.LoadAll();

    /// <summary>Domain projeleri üst katmanlara bağlı değil.</summary>
    [Fact]
    public void Domain_projects_do_not_reference_outer_layers()
    {
        var violations = Projects
            .Where(p => p.Name.EndsWith(".Domain", StringComparison.Ordinal))
            .SelectMany(p => p.References
                .Where(r => r.EndsWith(".Application", StringComparison.Ordinal)
                    || r.EndsWith(".Infrastructure", StringComparison.Ordinal)
                    || r.EndsWith(".Api", StringComparison.Ordinal)
                    || r.StartsWith("TahaKonsept.Web", StringComparison.Ordinal)
                    || r.StartsWith("TahaKonsept.Agent", StringComparison.Ordinal))
                .Select(r => $"{p.Name} → {r}"))
            .ToList();

        violations.ShouldBeEmpty(string.Join("\n", violations));
    }

    /// <summary>Application projeleri Infrastructure/Api'ye bağlı değil.</summary>
    [Fact]
    public void Application_projects_do_not_reference_infrastructure_or_api()
    {
        var violations = Projects
            .Where(p => p.Name.EndsWith(".Application", StringComparison.Ordinal))
            .SelectMany(p => p.References
                .Where(r => r.EndsWith(".Infrastructure", StringComparison.Ordinal) || r.EndsWith(".Api", StringComparison.Ordinal))
                .Select(r => $"{p.Name} → {r}"))
            .ToList();

        violations.ShouldBeEmpty(string.Join("\n", violations));
    }

    /// <summary>Infrastructure projeleri Api katmanına bağlı değil.</summary>
    [Fact]
    public void Infrastructure_projects_do_not_reference_api()
    {
        var violations = Projects
            .Where(p => p.Name.EndsWith(".Infrastructure", StringComparison.Ordinal))
            .SelectMany(p => p.References.Where(r => r.EndsWith(".Api", StringComparison.Ordinal)).Select(r => $"{p.Name} → {r}"))
            .ToList();

        violations.ShouldBeEmpty(string.Join("\n", violations));
    }

    /// <summary>Shared.Contracts bağımsızdır.</summary>
    [Fact]
    public void Shared_contracts_has_no_project_references()
    {
        var contracts = Projects.Where(p => p.Name == "TahaKonsept.Shared.Contracts").ToList();
        contracts.ShouldNotBeEmpty();
        contracts.SelectMany(p => p.References).ShouldBeEmpty();
    }

    /// <summary>BuildingBlocks modüllere ve host'lara bağlı değil.</summary>
    [Fact]
    public void Building_blocks_do_not_reference_modules_or_hosts()
    {
        var violations = Projects
            .Where(p => p.Name.StartsWith("TahaKonsept.BuildingBlocks.", StringComparison.Ordinal))
            .SelectMany(p => p.References
                .Where(r => r.StartsWith("TahaKonsept.Modules.", StringComparison.Ordinal)
                    || r is "TahaKonsept.Api" or "TahaKonsept.Web"
                    || r.StartsWith("TahaKonsept.Agent", StringComparison.Ordinal))
                .Select(r => $"{p.Name} → {r}"))
            .ToList();

        violations.ShouldBeEmpty(string.Join("\n", violations));
    }

    /// <summary>Bir modül başka modülün Domain/Infrastructure/Api projesine referans vermez.</summary>
    [Fact]
    public void Modules_do_not_reference_other_modules_internals()
    {
        var violations = new List<string>();
        foreach (var project in Projects)
        {
            var own = ModuleName().Match(project.Name);
            foreach (var reference in project.References)
            {
                var target = ModuleName().Match(reference);
                if (!target.Success)
                {
                    continue;
                }

                var sameModule = own.Success && own.Groups["module"].Value == target.Groups["module"].Value;
                var layer = target.Groups["layer"].Value;
                var forbiddenLayer = layer is "Domain" or "Infrastructure" or "Api";
                var isHost = project.Name == "TahaKonsept.Api";

                if (own.Success && !sameModule && forbiddenLayer)
                {
                    violations.Add($"{project.Name} → {reference}");
                }
                else if (!own.Success && !isHost && layer is "Domain" or "Infrastructure")
                {
                    // Host dışındaki modül-dışı projeler (Web, ajan) modüllerin iç katmanlarını görmez.
                    violations.Add($"{project.Name} → {reference}");
                }
            }
        }

        violations.ShouldBeEmpty(string.Join("\n", violations));
    }

    /// <summary>BuildingBlocks.Domain derlemesi EF/ASP.NET/altyapıya bağlı değil (NetArchTest).</summary>
    [Fact]
    public void Building_block_domain_assembly_is_pure()
    {
        var result = Types.InAssembly(typeof(Result).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "TahaKonsept.BuildingBlocks.Application",
                "TahaKonsept.BuildingBlocks.Infrastructure",
                "TahaKonsept.BuildingBlocks.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>BuildingBlocks.Application derlemesi EF/ASP.NET/altyapıya bağlı değil (NetArchTest).</summary>
    [Fact]
    public void Building_block_application_assembly_has_no_infrastructure_dependency()
    {
        var result = Types.InAssembly(typeof(IClock).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "TahaKonsept.BuildingBlocks.Infrastructure",
                "TahaKonsept.BuildingBlocks.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Shared.Contracts derlemesi hiçbir TahaKonsept derlemesine bağlı değil (NetArchTest).</summary>
    [Fact]
    public void Shared_contracts_assembly_is_standalone()
    {
        var result = Types.InAssembly(typeof(StageCommand).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("TahaKonsept.BuildingBlocks", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Infrastructure derlemesi Api katmanına bağlı değil (NetArchTest).</summary>
    [Fact]
    public void Building_block_infrastructure_assembly_does_not_depend_on_api()
    {
        var result = Types.InAssembly(typeof(SystemClock).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("TahaKonsept.BuildingBlocks.Api", "Microsoft.AspNetCore.Mvc")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [GeneratedRegex(@"^TahaKonsept\.Modules\.(?<module>[A-Za-z0-9]+)\.(?<layer>[A-Za-z0-9]+)")]
    private static partial Regex ModuleName();
}
