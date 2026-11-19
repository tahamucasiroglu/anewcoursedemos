using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using OkulYonetim.Application;
using OkulYonetim.Domain;
using OkulYonetim.Infrastructure;
using OkulYonetim.Persistence;
using OkulYonetim.WebAPI;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Yasak bağımlılıkların mimari testleri (docs/architecture.md, spec 0001 AC-9): Domain kodu EF Core ve üst
/// katmanları kullanmaz (ADR-0005); Application alt katmanlara ve dış SDK'lara bağlanmaz; controller'lar veritabanına
/// dokunmaz; Persistence/Infrastructure API'ye bağlanmaz; Npgsql yalnızca Persistence'ta.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(DomainAssemblyMarker).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(ApplicationServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly PersistenceAssembly = typeof(PersistenceServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly WebApiAssembly = typeof(Program).Assembly;

    private static readonly ArchModel Architecture = new ArchLoader()
        .LoadAssemblies(
            DomainAssembly,
            ApplicationAssembly,
            PersistenceAssembly,
            InfrastructureAssembly,
            WebApiAssembly,
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
        .Build();

    [Fact]
    public void Domain_DoesNotUseEfCoreOrOuterLayers()
    {
        IArchRule rule = Types().That().ResideInAssembly(DomainAssembly)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore.*"))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(ApplicationAssembly, PersistenceAssembly, InfrastructureAssembly, WebApiAssembly))
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    [Fact]
    public void Application_DoesNotDependOnLowerLayersOrExternalSdks()
    {
        IArchRule rule = Types().That().ResideInAssembly(ApplicationAssembly)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(PersistenceAssembly, InfrastructureAssembly, WebApiAssembly))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(Microsoft\.EntityFrameworkCore|Npgsql|Serilog).*"));
        rule.Check(Architecture);
    }

    [Fact]
    public void Controllers_DoNotTouchDatabase()
    {
        IArchRule rule = Types().That().ResideInNamespace("OkulYonetim.WebAPI.Controllers")
            .Should().NotDependOnAny(Types().That().ResideInAssembly(PersistenceAssembly))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(Microsoft\.EntityFrameworkCore|Npgsql).*"));
        rule.Check(Architecture);
    }

    [Fact]
    public void PersistenceAndInfrastructure_DoNotDependOnWebApi()
    {
        IArchRule rule = Types().That().ResideInAssembly(PersistenceAssembly, InfrastructureAssembly)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(WebApiAssembly));
        rule.Check(Architecture);
    }

    [Fact]
    public void Npgsql_IsUsedOnlyInPersistence()
    {
        IArchRule rule = Types().That().ResideInAssembly(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, WebApiAssembly)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Npgsql.*"));
        rule.Check(Architecture);
    }
}
