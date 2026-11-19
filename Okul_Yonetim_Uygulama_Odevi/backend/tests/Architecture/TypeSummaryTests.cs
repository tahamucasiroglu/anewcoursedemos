using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using OkulYonetim.Application;
using OkulYonetim.Domain;
using OkulYonetim.Infrastructure;
using OkulYonetim.Persistence;
using OkulYonetim.WebAPI;
using Shouldly;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// "Her tipin başında Türkçe açıklama" kuralının testi (spec 0001 AC-8): derlenen XML belgelerinde her elle yazılmış
/// tipin boş olmayan bir summary'si olmalı. Üye summary'si aranmaz; üretilmiş kod (migration'lar) muaftır.
/// </summary>
public sealed class TypeSummaryTests
{
    public static TheoryData<string> Assemblies() =>
    [
        typeof(DomainAssemblyMarker).Assembly.Location,
        typeof(ApplicationServiceRegistration).Assembly.Location,
        typeof(PersistenceServiceRegistration).Assembly.Location,
        typeof(InfrastructureServiceRegistration).Assembly.Location,
        typeof(Program).Assembly.Location,
    ];

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void EveryType_HasNonEmptySummary(string assemblyPath)
    {
        Assembly assembly = Assembly.LoadFrom(assemblyPath);
        string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        File.Exists(xmlPath).ShouldBeTrue($"XML belge dosyası yok: {xmlPath}");

        HashSet<string> documented = XDocument.Load(xmlPath)
            .Descendants("member")
            .Where(m => ((string?)m.Attribute("name"))?.StartsWith("T:", StringComparison.Ordinal) == true)
            .Where(m => !string.IsNullOrWhiteSpace(m.Element("summary")?.Value))
            .Select(m => ((string)m.Attribute("name")!)[2..])
            .ToHashSet(StringComparer.Ordinal);

        string[] missing = assembly.GetTypes()
            .Where(IsHandWritten)
            .Select(t => t.FullName!.Replace('+', '.'))
            .Where(name => !documented.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToArray();

        missing.ShouldBeEmpty($"Summary'si olmayan tipler ({assembly.GetName().Name}): {string.Join(", ", missing)}");
    }

    private static bool IsHandWritten(Type type) =>
        type.Namespace?.StartsWith("OkulYonetim", StringComparison.Ordinal) == true
        && !type.Namespace.Contains(".Migrations", StringComparison.Ordinal)
        && !type.Name.StartsWith('<')
        && type.GetCustomAttribute<CompilerGeneratedAttribute>() is null
        && type.GetCustomAttribute<GeneratedCodeAttribute>() is null;
}
