using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Konsept.ArchitectureTests.Rules;

/// <summary>
/// docs/conventions.md'deki yapısal kodlama kuralları (K-1 iç içe tip, K-2 Abstract → Base → Concrete,
/// K-4 ince controller). Her kural ihlallerin listesini döndürür; boş liste = kural sağlanıyor.
/// </summary>
public static partial class StructureRules
{
    /// <summary>K-1: iç içe tip yasak (derleyici ve XAML/Razor üretimi hariç).</summary>
    /// <param name="assemblies">İncelenecek derlemeler.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> K1NoNestedTypes(IEnumerable<Assembly> assemblies) =>
        [.. OwnTypes(assemblies)
            .Where(t => t.IsNested)
            .Select(t => t.FullName ?? t.Name)
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// K-2: <c>Concrete</c> altındaki her sınıf <c>Base</c> altındaki bir <c>…Base</c> sınıfından türer ve
    /// <c>Abstract</c> altındaki bir interface'i uygular; davranış taşıyan adlar (Service, Repository…) da
    /// yalnızca <c>Concrete</c> altında olabilir.
    /// </summary>
    /// <param name="assemblies">İncelenecek derlemeler.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> K2AbstractBaseConcrete(IEnumerable<Assembly> assemblies)
    {
        var violations = new List<string>();
        foreach (var type in OwnTypes(assemblies).Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var inConcrete = HasSegment(type.Namespace, "Concrete");
            if (inConcrete)
            {
                if (!BaseChain(type).Any(b => PlainName(b).EndsWith("Base", StringComparison.Ordinal) && HasSegment(b.Namespace, "Base")))
                {
                    violations.Add($"{type.FullName}: Base altındaki bir …Base sınıfından türemiyor");
                }

                if (!type.GetInterfaces().Any(i => HasSegment(i.Namespace, "Abstract")))
                {
                    violations.Add($"{type.FullName}: Abstract altındaki bir interface'i uygulamıyor");
                }
            }
            else if (BehaviourName().IsMatch(type.Name) && !IsFrameworkAdapter(type))
            {
                violations.Add($"{type.FullName}: davranış taşıyan sınıf Concrete altında değil");
            }
        }

        return [.. violations.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// K-4: controller'lar mantık taşımaz — kendi private/protected metodu yoktur ve alanları yalnızca
    /// readonly interface bağımlılıklarıdır (tek satırlık eylem gövdesi review'da kontrol edilir).
    /// </summary>
    /// <param name="assemblies">İncelenecek derlemeler.</param>
    /// <returns>İhlaller.</returns>
    public static IReadOnlyList<string> K4ThinControllers(IEnumerable<Assembly> assemblies)
    {
        const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.Public | BindingFlags.NonPublic;
        var violations = new List<string>();
        foreach (var type in OwnTypes(assemblies).Where(IsController))
        {
            foreach (var method in type.GetMethods(Declared).Where(m => !m.IsPublic && !m.IsSpecialName && !IsGenerated(m)))
            {
                violations.Add($"{type.FullName}.{method.Name}: controller'da mantık metodu");
            }

            foreach (var field in type.GetFields(Declared).Where(f => !IsGenerated(f)))
            {
                if (!field.IsInitOnly || !field.FieldType.IsInterface)
                {
                    violations.Add($"{type.FullName}.{field.Name}: controller alanı readonly interface değil");
                }
            }
        }

        return [.. violations.Order(StringComparer.Ordinal)];
    }

    private static bool IsController(Type type) =>
        BaseChain(type).Any(b => b.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase");

    private static bool IsFrameworkAdapter(Type type) =>
        BaseChain(type).Any(b => b.Namespace?.StartsWith("Konsept.", StringComparison.Ordinal) == false
            && b != typeof(object));

    private static IEnumerable<Type> BaseChain(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static string PlainName(Type type)
    {
        var tick = type.Name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? type.Name : type.Name[..tick];
    }

    private static bool HasSegment(string? ns, string segment) =>
        ns is not null && ns.Split('.').Contains(segment, StringComparer.Ordinal);

    private static IEnumerable<Type> OwnTypes(IEnumerable<Assembly> assemblies) =>
        assemblies.SelectMany(SafeTypes)
            .Where(t => t.Namespace?.StartsWith("Konsept", StringComparison.Ordinal) == true)
            .Where(t => !IsGenerated(t) && !GeneratedName().IsMatch(t.Name));

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static bool IsGenerated(MemberInfo member) =>
        member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || member.Name.Contains('<', StringComparison.Ordinal);

    [GeneratedRegex(@"(Service|Repository|Engine|Provider|Client|Channel|Registry|Launcher|Probe)$")]
    private static partial Regex BehaviourName();

    [GeneratedRegex(@"[<>!$@]|^XamlIl|^__")]
    private static partial Regex GeneratedName();
}
