using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Account.Abstract;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.EngineList;
using TahaKonsept.Agent.EngineList.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Tests.Fakes;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.Agent.WebSite.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Agent.Tests.ViewModels;

/// <summary>Motor satırı ("Kur" + ilerleme + VRAM uyarısı) ve durum penceresi (bağlantı, hesap, iş, eşleme kaldırma).</summary>
public sealed class EngineAndStatusViewModelTests
{
    /// <summary>Test: Engine install reports progress and refreshes installed state.</summary>
    [Fact]
    public async Task Engine_install_reports_progress_and_refreshes_installed_state()
    {
        var installed = false;
        var engine = Substitute.For<IAgentEngine>();
        engine.Key.Returns("supertonic-3");
        engine.DisplayName.Returns("Supertonic 3");
        engine.MinVramMb.Returns((int?)null);
        engine.IsInstalled().Returns(_ => installed);
        engine.EnsureReadyAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            ci.ArgAt<IProgress<EngineSetupProgress>?>(0)!.Report(new EngineSetupProgress("Model indiriliyor", 55));
            installed = true;
            return Task.FromResult(Result.Success());
        });
        using var vm = new EngineItemViewModel(new EngineDescriptor(engine, EngineCategory.Speech), new ImmediateUiDispatcher(), NullLogger.Instance);

        vm.IsInstalled.ShouldBeFalse();
        vm.StatusText.ShouldBe("Kurulu değil");
        vm.VramText.ShouldBe("GPU gerekmez");
        vm.CategoryText.ShouldBe("Ses");
        vm.InstallCommand.CanExecute(null).ShouldBeTrue();

        await vm.InstallCommand.ExecuteAsync(null);

        vm.IsInstalled.ShouldBeTrue();
        vm.Percent.ShouldBe(100);
        vm.Message.ShouldBe("Hazır.");
        vm.InstallCommand.CanExecute(null).ShouldBeFalse();
    }

    /// <summary>Test: Engine install failure shows message.</summary>
    [Fact]
    public async Task Engine_install_failure_shows_message()
    {
        var engine = Substitute.For<IAgentEngine>();
        engine.Key.Returns("chatterbox");
        engine.DisplayName.Returns("Chatterbox");
        engine.MinVramMb.Returns(4096);
        engine.EnsureReadyAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("engine.failed", "Python bulunamadı"))));
        using var vm = new EngineItemViewModel(new EngineDescriptor(engine, EngineCategory.Speech), new ImmediateUiDispatcher(), NullLogger.Instance);

        vm.VramText.ShouldBe("4 GB VRAM");
        vm.SetGpuVram(2048);
        vm.IsVramInsufficient.ShouldBeTrue();
        vm.SetGpuVram(6144);
        vm.IsVramInsufficient.ShouldBeFalse();

        await vm.InstallCommand.ExecuteAsync(null);
        vm.IsInstalled.ShouldBeFalse();
        vm.Message!.ShouldContain("Python bulunamadı");
    }

    /// <summary>Test: Status reflects hub account job and unpair.</summary>
    [Fact]
    public void Status_reflects_hub_account_job_and_unpair()
    {
        var hub = Substitute.For<IAgentHubConnection>();
        hub.Status.Returns(HubConnectionStatus.Connecting);
        var auth = Substitute.For<IAgentAuthService>();
        auth.IsPaired.Returns(true);
        var account = Substitute.For<IPairedAccountReader>();
        account.GetAccountName().Returns("taha");
        var catalog = Substitute.For<IEngineCatalog>();
        catalog.GetEngines().Returns([]);
        var jobs = Substitute.For<IJobRunner>();
        jobs.GetActiveStages().Returns([]);
        var startup = Substitute.For<IStartupRegistration>();
        startup.IsEnabled.Returns(true);
        var pairing = new FakePairingService();
        using var vm = new StatusViewModel(
            hub, pairing, auth, account, catalog, Substitute.For<ICapabilityDetector>(), jobs, Substitute.For<IAgentPaths>(),
            Substitute.For<IDesktopLauncher>(), startup, Substitute.For<IWebSiteLocator>(), new ImmediateUiDispatcher(),
            NullLogger<StatusViewModel>.Instance);

        vm.HubStatusText.ShouldBe("Bağlanıyor…");
        vm.AccountText.ShouldBe("taha");
        vm.StartWithWindows.ShouldBeTrue();
        vm.JobSummaryText.ShouldBe("Şu an çalışan iş yok.");

        hub.StatusChanged += Raise.Event<EventHandler<HubConnectionStatus>>(hub, HubConnectionStatus.Connected);
        vm.IsConnected.ShouldBeTrue();

        var stage = new RunningStageInfo(Guid.NewGuid(), Guid.NewGuid(), PipelineStage.SceneRender, 2, 42, "Kareler", DateTimeOffset.UtcNow);
        jobs.GetActiveStages().Returns([stage]);
        jobs.StageUpdated += Raise.Event<EventHandler<RunningStageInfo>>(jobs, stage);
        vm.HasActiveJob.ShouldBeTrue();
        vm.CurrentJobText.ShouldBe("Sahne render (sahne 3) · %42 · Kareler");
        vm.CurrentJobPercent.ShouldBe(42);

        startup.SetEnabled(false).Returns(Result.Success());
        vm.StartWithWindows = false;
        startup.Received(1).SetEnabled(false);
        vm.StartWithWindows.ShouldBeFalse();

        vm.UnpairCommand.CanExecute(null).ShouldBeTrue();
        auth.IsPaired.Returns(false);
        vm.UnpairCommand.Execute(null);
        pairing.Unpaired.ShouldBeTrue();
        vm.AccountText.ShouldBe("Eşlenmedi");
        vm.PairCommand.CanExecute(null).ShouldBeTrue();
    }

    /// <summary>Test: Status load sets gpu summary and vram warnings.</summary>
    [Fact]
    public async Task Status_load_sets_gpu_summary_and_vram_warnings()
    {
        var engine = Substitute.For<IAgentEngine>();
        engine.Key.Returns("fish-s2-pro");
        engine.DisplayName.Returns("Fish S2 Pro");
        engine.MinVramMb.Returns(12288);
        var catalog = Substitute.For<IEngineCatalog>();
        catalog.GetEngines().Returns([new EngineDescriptor(engine, EngineCategory.Speech)]);
        var caps = Substitute.For<ICapabilityDetector>();
        caps.DetectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AgentCapabilities
        {
            Gpus = [new GpuInfo { Name = "RTX 3060 Laptop", VramMb = 6144, CudaAvailable = true }],
        }));
        var jobs = Substitute.For<IJobRunner>();
        jobs.GetActiveStages().Returns([]);
        using var vm = new StatusViewModel(
            Substitute.For<IAgentHubConnection>(), new FakePairingService(), Substitute.For<IAgentAuthService>(), Substitute.For<IPairedAccountReader>(),
            catalog, caps, jobs, Substitute.For<IAgentPaths>(), Substitute.For<IDesktopLauncher>(), Substitute.For<IStartupRegistration>(),
            Substitute.For<IWebSiteLocator>(), new ImmediateUiDispatcher(), NullLogger<StatusViewModel>.Instance);

        await vm.LoadAsync(CancellationToken.None);

        vm.GpuSummary.ShouldContain("6 GB VRAM");
        vm.Engines.Single().IsVramInsufficient.ShouldBeTrue();
    }
}
