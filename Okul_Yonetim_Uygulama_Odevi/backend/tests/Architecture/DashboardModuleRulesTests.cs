using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using OkulYonetim.Application;
using OkulYonetim.Application.Features.Billing.Reports;
using OkulYonetim.Application.Features.Dashboard.Summary;
using OkulYonetim.Domain;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Yasak bağımlılık 4 ve plan 0017 Risks 3 (S1): yönetici paneli (Dashboard) yalnızca okur ve diğer modüllerin verisine yalnızca onların
/// sorgularıyla (Mediator) erişir — hiçbir depo arayüzüne (<c>Application.Ports.I…Repository</c>) bağımlı olmaz; kurum/şube filtresi o
/// modüllerin depolarında oturumdan uygulanır. Pozitif kontrol: kural kümesi dolu ve depo kullanan bir işleyici kurala takılır.
/// </summary>
public sealed class DashboardModuleRulesTests
{
    private const string DashboardNamespaces = @"^OkulYonetim\.Application\.Features\.Dashboard(\..*)?$";

    private static readonly ArchModel Architecture = new ArchLoader()
        .LoadAssemblies(typeof(DomainAssemblyMarker).Assembly, typeof(ApplicationServiceRegistration).Assembly)
        .Build();

    [Fact]
    public void Dashboard_DoesNotDependOnAnyRepository()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(DashboardNamespaces)
            .Should().NotDependOnAny(Repositories());
        rule.Check(Architecture);
    }

    [Fact]
    public void PositiveControl_RuleCatchesRepositoryUse()
    {
        Types().That().ResideInNamespaceMatching(DashboardNamespaces).GetObjects(Architecture).Count().ShouldBeGreaterThan(10);
        Repositories().GetObjects(Architecture).Count().ShouldBeGreaterThan(10);
        Types().That().HaveFullName(typeof(SummaryHandlers).FullName!).GetObjects(Architecture).ShouldHaveSingleItem();

        TypesShouldConjunction mustFail = Types().That().HaveFullName(typeof(BillingReportHandlers).FullName!)
            .Should().NotDependOnAny(Repositories());
        mustFail.HasNoViolations(Architecture).ShouldBeFalse("tahsilat okuma işleyicisi depoyu kullanır; kural onu yakalamalı");
    }

    private static GivenTypesConjunction Repositories() =>
        Types().That().ResideInNamespace("OkulYonetim.Application.Ports").And().HaveNameEndingWith("Repository");
}
