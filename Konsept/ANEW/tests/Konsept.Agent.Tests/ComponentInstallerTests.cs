using Konsept.Agent.Application.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Eksik bileşen kurulumu (spec 0027 AC-7): kurulabilir bilgisi ajanın listesinden; kurulum komutu gerçek kurulum motoruyla (sahte indirici,
/// gerçek hash/zip) yalnızca listedeki kimlikleri kurar; isteğe bağlı bileşen ilk kurulumda indirilmez, istenince kurulur. Gerçek ağ yok.
/// </summary>
public sealed class ComponentInstallerTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-install-");
    private readonly FakeSetupHost _host = new();
    private readonly List<SetupEvent> _events = [];
    private readonly SetupComponent _core;
    private readonly SetupComponent _model;

    /// <summary>Initializes a new instance of the <see cref="ComponentInstallerTests"/> class.</summary>
    public ComponentInstallerTests()
    {
        _core = _host.Add("core");
        _model = _host.Add("model", required: false);
    }

    private string Root => Path.Combine(_root.FullName, "anewconcept");

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>İsteğe bağlı bileşen ilk kurulumda Atlandı kalır ve indirilmez; çekirdek kurulunca ajan Hazır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task OptionalComponent_SkippedByFirstSetup()
    {
        var setup = await CreateAsync();
        setup.Components.Select(c => c.Status).ShouldBe([ComponentStatus.Waiting, ComponentStatus.Skipped]);

        await setup.RunAsync(TestContext.Current.CancellationToken);

        setup.Components.Select(c => c.Status).ShouldBe([ComponentStatus.Installed, ComponentStatus.Skipped]);
        _host.Downloads.ShouldBe([_core.Url]);
        setup.IsReady.ShouldBeTrue();
        (await Create().InspectAsync(TestContext.Current.CancellationToken)).ShouldBeFalse(); // Sonraki açılışta kurulum ekranı gerekmez.
    }

    /// <summary>Describe yalnızca listedeki kimlikleri ad ve boyutlarıyla verir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Describe_OnlyListedComponents()
    {
        var installer = new ComponentInstaller(await CreateAsync(), TimeProvider.System);

        installer.Describe(["model", "supertonic-3"]).ShouldBe([new AgentComponentInfo("model", "MODEL", 1000, 2000, "MIT")]); // A-2 (plan 0028): lisans da bildirilir.
    }

    /// <summary>Kurulum komutu istenen isteğe bağlı bileşeni kurar (diğerine dokunmaz), Hazır olayı çıkar, sonuç başarılı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_RequestedOptional_Installed()
    {
        var ct = TestContext.Current.CancellationToken;
        var setup = await CreateAsync();
        await setup.RunAsync(ct);
        _host.Downloads.Clear();
        _events.Clear();
        var installer = new ComponentInstaller(setup, TimeProvider.System);

        var reply = await installer.InstallAsync(new AgentInstallRequest(["model"]), ct);

        (reply.Ok, reply.Failed.Count).ShouldBe((true, 0));
        _host.Downloads.ShouldBe([_model.Url]);
        setup.Components.Single(c => c.Component.Id == "model").Status.ShouldBe(ComponentStatus.Installed);
        new FileSetupStateStore(Root).Load().Installed.Keys.Order(StringComparer.Ordinal).ShouldBe(["core", "model"]);
        _events.ShouldContain(e => e.Kind == SetupEventKind.Ready);
    }

    /// <summary>Listede olmayan kimlik varsa hiçbir şey kurulmaz ve kimlik başarısız bildirilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_UnknownId_NothingInstalled()
    {
        var ct = TestContext.Current.CancellationToken;
        var installer = new ComponentInstaller(await CreateAsync(), TimeProvider.System);

        var reply = await installer.InstallAsync(new AgentInstallRequest(["model", "evil"]), ct);

        (reply.Ok, string.Join(",", reply.Failed)).ShouldBe((false, "evil"));
        _host.Downloads.ShouldBeEmpty();
    }

    /// <summary>İndirme başarısızsa sonuç başarısız ve bileşen kimliğini sayar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_DownloadFails_ReportsFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        _host.Unreachable.Add(_model.Url);
        var installer = new ComponentInstaller(await CreateAsync(), TimeProvider.System);

        var reply = await installer.InstallAsync(new AgentInstallRequest(["model"]), ct);

        (reply.Ok, string.Join(",", reply.Failed)).ShouldBe((false, "model"));
    }

    /// <summary>İlk kurulum sürerken gelen komut onun bitmesini bekler, sonra kurar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_WhileFirstSetupRuns_Waits()
    {
        var ct = TestContext.Current.CancellationToken;
        var setup = await CreateAsync();
        var hold = new TaskCompletionSource();
        _host.Hold = hold;
        var first = setup.RunAsync(ct);
        await WaitUntilAsync(() => setup.IsRunning, ct);
        var installer = new ComponentInstaller(setup, TimeProvider.System);

        var install = installer.InstallAsync(new AgentInstallRequest(["model"]), ct);
        await Task.Delay(200, ct);
        install.IsCompleted.ShouldBeFalse();
        hold.SetResult();
        await first;
        var reply = await install;

        reply.Ok.ShouldBeTrue();
        setup.Components.Select(c => c.Status).ShouldBe([ComponentStatus.Installed, ComponentStatus.Installed]);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, cancellationToken);
        }

        condition().ShouldBeTrue();
    }

    private SetupCoordinator Create()
    {
        var setup = new SetupCoordinator([_core, _model], _host, new FileSetupStateStore(Root), Root, Path.Combine(_root.FullName, "Videolar"), null);
        setup.Changed += (_, e) => _events.Add(e);
        return setup;
    }

    private async Task<SetupCoordinator> CreateAsync()
    {
        var setup = Create();
        await setup.InspectAsync(TestContext.Current.CancellationToken);
        return setup;
    }
}
