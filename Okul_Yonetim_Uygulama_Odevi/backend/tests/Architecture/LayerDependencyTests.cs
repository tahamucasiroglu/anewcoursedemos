using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using OkulYonetim.Application;
using OkulYonetim.Domain;
using OkulYonetim.Infrastructure;
using OkulYonetim.Persistence;
using OkulYonetim.WebAPI;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Yasak bağımlılıkların mimari testleri (docs/architecture.md, spec 0001 AC-9): Domain kodu EF Core ve üst
/// katmanları kullanmaz (ADR-0005); Application alt katmanlara ve dış SDK'lara bağlanmaz; API katmanı veritabanı
/// bağlamına ve EF Core'a dokunmaz; Persistence/Infrastructure API'ye bağlanmaz; Npgsql yalnızca Persistence'ta.
/// Dış paket kümeleri referans edilen tiplerle kurulur (<c>Types(true)</c>); aksi halde yüklenmemiş paketlere bakan
/// kural sessizce boş geçer (Known traps T8). Pozitif kontroller kümelerin gerçekten dolu olduğunu kanıtlar.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string EfCore = @"^Microsoft\.EntityFrameworkCore(\..*)?$";
    private const string Npgsql = @"^Npgsql(\..*)?$";
    private const string Serilog = @"^Serilog(\..*)?$";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(DomainAssemblyMarker).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(ApplicationServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly PersistenceAssembly = typeof(PersistenceServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly WebApiAssembly = typeof(Program).Assembly;

    private static readonly ArchModel Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, PersistenceAssembly, InfrastructureAssembly, WebApiAssembly)
        .Build();

    [Fact]
    public void Domain_DoesNotUseEfCoreOrOuterLayers()
    {
        IArchRule rule = Types().That().ResideInAssembly(DomainAssembly)
            .Should().NotDependOnAny(External(EfCore))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(ApplicationAssembly, PersistenceAssembly, InfrastructureAssembly, WebApiAssembly))
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    [Fact]
    public void Application_DoesNotDependOnLowerLayersOrExternalSdks()
    {
        IArchRule rule = Types().That().ResideInAssembly(ApplicationAssembly)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(PersistenceAssembly, InfrastructureAssembly, WebApiAssembly))
            .AndShould().NotDependOnAny(External(EfCore))
            .AndShould().NotDependOnAny(External(Npgsql))
            .AndShould().NotDependOnAny(External(Serilog));
        rule.Check(Architecture);
    }

    [Fact]
    public void WebApi_DoesNotTouchDbContextOrEfCore()
    {
        IArchRule rule = Types().That().ResideInAssembly(WebApiAssembly)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("OkulYonetim.Persistence.Contexts"))
            .AndShould().NotDependOnAny(External(EfCore))
            .AndShould().NotDependOnAny(External(Npgsql));
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
            .Should().NotDependOnAny(External(Npgsql));
        rule.Check(Architecture);
    }

    [Theory]
    [InlineData(typeof(OkulYonetim.Infrastructure.Logging.SerilogLoggerAdapter), Serilog)]
    [InlineData(typeof(OkulYonetim.Persistence.HealthChecks.NpgsqlDatabaseHealthProbe), EfCore)]
    [InlineData(typeof(OkulYonetim.Persistence.Contexts.OkulDbContext), EfCore)]
    public void PositiveControl_ExternalTypeSetsAreNotEmpty(System.Type knownUserType, string externalNamespace)
    {
        // Bu tipler o pakete gerçekten bağımlı: "bağımlı olmamalı" kuralı ihlal vermeli. Vermiyorsa küme boştur ve
        // yukarıdaki yasak kuralları hiçbir zaman kırmızıya düşemez.
        // Tip derleyiciyle verilir (typeof): yeniden adlandırılırsa derleme kırılır, kontrol boşuna yeşil kalmaz.
        ArgumentNullException.ThrowIfNull(knownUserType);
        string knownUser = knownUserType.FullName!;
        Types().That().HaveFullName(knownUser).GetObjects(Architecture).ShouldNotBeEmpty($"{knownUser} mimaride bulunmalı");

        TypesShouldConjunction mustFail = Types().That().HaveFullName(knownUser)
            .Should().NotDependOnAny(External(externalNamespace));

        mustFail.HasNoViolations(Architecture).ShouldBeFalse($"{knownUser} → {externalNamespace} bağımlılığı görülmeli");
    }

    private static GivenTypesConjunction External(string namespacePattern) =>
        Types(true).That().ResideInNamespaceMatching(namespacePattern);
}
