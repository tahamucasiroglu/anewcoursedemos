using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace Konsept.ArchitectureTests.Rules;

/// <summary>
/// docs/architecture.md'deki yasak bağımlılıklar (Y-1…Y-6). Her kural ihlallerin listesini döndürür;
/// boş liste = kural sağlanıyor. Bağımlılıklar IL düzeyinde (metot gövdeleri dahil) ArchUnitNET'ten okunur.
/// </summary>
public static partial class DependencyRules
{
    /// <summary>Y-1: Bir modül başka bir modülün yalnızca <c>Contracts</c> katmanına referans verebilir.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y1ModulesTalkOnlyThroughContracts(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
        {
            var from = ModuleOf(origin);
            var to = ModuleOf(target);
            return from is not null && to is not null
                && from.Value.Module != to.Value.Module
                && to.Value.Layer != "Contracts";
        });

    /// <summary>Y-2: <c>Domain</c> yalnızca <c>System</c>, <c>Shared.Kernel</c> ve kendi modülünün Domain'ine bağımlıdır.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y2DomainIsPure(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
        {
            var from = ModuleOf(origin);
            if (from is null || from.Value.Layer != "Domain")
            {
                return false;
            }

            var ns = target.Namespace.FullName;
            var to = ModuleOf(target);
            var allowed = ns.Length == 0
                || ns == "System"
                || ns.StartsWith("System.", StringComparison.Ordinal)
                || ns.StartsWith("Konsept.Shared.Kernel", StringComparison.Ordinal)
                || (to is not null && to.Value.Module == from.Value.Module && to.Value.Layer == "Domain");
            return !allowed;
        });

    /// <summary>Y-3: <c>Application</c>, <c>Infrastructure</c>'a referans veremez.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y3ApplicationDoesNotUseInfrastructure(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
            ModuleOf(origin)?.Layer == "Application" && ModuleOf(target)?.Layer == "Infrastructure");

    /// <summary>Y-4: Hiçbir modül başka bir modülün <c>DbContext</c>'ini kullanamaz.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y4NoForeignDbContext(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
        {
            var to = ModuleOf(target);
            if (to is null || !target.Name.EndsWith("DbContext", StringComparison.Ordinal))
            {
                return false;
            }

            var from = ModuleOf(origin);
            return from is not null && from.Value.Module != to.Value.Module;
        });

    /// <summary>Y-5: <c>Konsept.Web</c> modüllere ve veritabanına referans veremez; yalnızca ApiClient'ı kullanır.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y5WebUsesOnlyApiClient(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
        {
            var ns = origin.Namespace.FullName;
            var isWeb = (ns == "Konsept.Web" || ns.StartsWith("Konsept.Web.", StringComparison.Ordinal))
                && !ns.StartsWith("Konsept.Web.ApiClient", StringComparison.Ordinal);
            if (!isWeb)
            {
                return false;
            }

            var t = target.Namespace.FullName;
            return t.StartsWith("Konsept.Modules.", StringComparison.Ordinal)
                || t.StartsWith("Konsept.Shared.Infrastructure", StringComparison.Ordinal)
                || t.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || t.StartsWith("Npgsql", StringComparison.Ordinal);
        });

    /// <summary>Y-6: Controller'lar repository'ye, <c>DbContext</c>'e ya da EF Core'a bağımlı olamaz.</summary>
    /// <param name="architecture">İncelenecek mimari.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> Y6ControllersUseOnlyServices(Architecture architecture) =>
        Violations(architecture, (origin, target) =>
            IsController(origin)
            && (target.Name.EndsWith("Repository", StringComparison.Ordinal)
                || target.Name.EndsWith("DbContext", StringComparison.Ordinal)
                || target.Namespace.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

    private static bool IsController(IType type) =>
        type is Class cls && cls.InheritedClasses.Any(c => c.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase");

    private static (string Module, string Layer)? ModuleOf(IType type)
    {
        var match = ModulePattern().Match(type.Namespace.FullName);
        return match.Success ? (match.Groups["module"].Value, match.Groups["layer"].Value) : null;
    }

    private static List<string> Violations(Architecture architecture, Func<IType, IType, bool> isViolation) =>
        [.. architecture.Types
            .Where(t => t.FullName.StartsWith("Konsept.", StringComparison.Ordinal))
            .SelectMany(t => t.Dependencies.Select(d => (Origin: t, d.Target)))
            .Where(p => isViolation(p.Origin, p.Target))
            .Select(p => $"{p.Origin.FullName} -> {p.Target.FullName}")
            .Distinct()
            .Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"^Konsept\.Modules\.(?<module>[^.]+)\.(?<layer>[^.]+)")]
    private static partial Regex ModulePattern();
}
