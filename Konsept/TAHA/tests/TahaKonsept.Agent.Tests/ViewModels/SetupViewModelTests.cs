using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Pairing;
using TahaKonsept.Agent.Core.Prerequisites;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Tests.Fakes;
using TahaKonsept.Agent.ViewModels;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using Xunit;

namespace TahaKonsept.Agent.Tests.ViewModels;

/// <summary>Kurulum penceresi durum makinesi: bileşenler → kurulum → (hata/tekrar) → eşleme → bitti.</summary>
public sealed class SetupViewModelTests
{
    private readonly IPrerequisiteInstaller _installer = Substitute.For<IPrerequisiteInstaller>();
    private readonly ICapabilityDetector _capabilities = Substitute.For<ICapabilityDetector>();
    private readonly IAgentAuthService _auth = Substitute.For<IAgentAuthService>();
    private readonly InMemoryAgentSettingsStore _agentSettings = new();
    private readonly InMemoryDesktopAppSettingsStore _desktopSettings = new();
    private readonly FakePairingService _pairing = new();
    private readonly Dictionary<string, bool> _installed = new()
    {
        ["node"] = false,
        ["ffmpeg"] = true,
        ["chrome-headless-shell"] = false,
        ["uv"] = false,
    };

    /// <summary>Sahteleri hazırlar.</summary>
    public SetupViewModelTests()
    {
        _installer.GetStatus().Returns(_ => _installed
            .Select(kv => SetupStepViewModelTests.Status(kv.Key, optional: kv.Key == "uv", installed: kv.Value))
            .ToList());
        _installer.AreRequiredInstalled().Returns(_ => _installed.Where(kv => kv.Key != "uv").All(kv => kv.Value));
    }

    /// <summary>Test: Loads steps and starts in prerequisites phase.</summary>
    [Fact]
    public void Loads_steps_and_starts_in_prerequisites_phase()
    {
        using var vm = Create();

        vm.Phase.ShouldBe(SetupPhase.Prerequisites);
        vm.Steps.Count.ShouldBe(4);
        vm.Steps.Single(s => s.Id == "ffmpeg").State.ShouldBe(SetupStepState.Installed);
        vm.Steps.Single(s => s.Id == "uv").State.ShouldBe(SetupStepState.Skipped);
        vm.InstallButtonText.ShouldBe("Kur ve devam et");
        vm.ServerUrl.ShouldBe("http://192.168.1.102:7111");
        vm.InstallCommand.CanExecute(null).ShouldBeTrue();
    }

    /// <summary>Test: Successful install moves to pairing and starts pairing.</summary>
    [Fact]
    public async Task Successful_install_moves_to_pairing_and_starts_pairing()
    {
        _pairing.WaitGate = new TaskCompletionSource();
        SucceedInstall();
        using var vm = Create();

        await vm.InstallCommand.ExecuteAsync(null);

        vm.Phase.ShouldBe(SetupPhase.Pairing);
        vm.IsPairingVisible.ShouldBeTrue();
        vm.CurrentStepNumber.ShouldBe(2);
        _pairing.StartCalls.ShouldBe(1);
        vm.Pairing.UserCode.ShouldBe("ABCD-EFGH");
        vm.Steps.Where(s => !s.IsOptional).ShouldAllBe(s => s.IsOk);
        vm.Steps.Single(s => s.Id == "node").State.ShouldBe(SetupStepState.Done);

        _pairing.WaitGate.SetResult();
        await WaitUntilAsync(() => vm.Phase == SetupPhase.Completed);
        _desktopSettings.Current.SetupCompleted.ShouldBeTrue();
        vm.FinishCommand.CanExecute(null).ShouldBeTrue();
    }

    /// <summary>Test: Already paired skips pairing and completes.</summary>
    [Fact]
    public async Task Already_paired_skips_pairing_and_completes()
    {
        _auth.IsPaired.Returns(true);
        SucceedInstall();
        using var vm = Create();

        await vm.InstallCommand.ExecuteAsync(null);

        vm.Phase.ShouldBe(SetupPhase.Completed);
        _pairing.StartCalls.ShouldBe(0);
        _desktopSettings.Current.SetupCompleted.ShouldBeTrue();
    }

    /// <summary>Test: Failed install goes to failed then retry succeeds.</summary>
    [Fact]
    public async Task Failed_install_goes_to_failed_then_retry_succeeds()
    {
        _installer.InstallAsync(Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<IProgress<PrerequisiteProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("prereq.failed", "Node.js indirilemedi"))));
        using var vm = Create();

        await vm.InstallCommand.ExecuteAsync(null);

        vm.Phase.ShouldBe(SetupPhase.Failed);
        vm.InstallButtonText.ShouldBe("Tekrar dene");
        vm.StatusMessage.ShouldContain("Node.js indirilemedi");
        vm.Steps.Single(s => s.Id == "node").State.ShouldBe(SetupStepState.Failed);
        vm.Logs.ShouldContain(l => l.Contains("HATA", StringComparison.Ordinal));
        vm.InstallCommand.CanExecute(null).ShouldBeTrue();

        _auth.IsPaired.Returns(true);
        SucceedInstall();
        await vm.InstallCommand.ExecuteAsync(null);
        vm.Phase.ShouldBe(SetupPhase.Completed);
    }

    /// <summary>Test: Required step still missing after success result is treated as failure.</summary>
    [Fact]
    public async Task Required_step_still_missing_after_success_result_is_treated_as_failure()
    {
        _installer.InstallAsync(Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<IProgress<PrerequisiteProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        using var vm = Create();

        await vm.InstallCommand.ExecuteAsync(null);

        vm.Phase.ShouldBe(SetupPhase.Failed);
    }

    /// <summary>Test: Only selected optional components are requested.</summary>
    [Fact]
    public async Task Only_selected_optional_components_are_requested()
    {
        IReadOnlyCollection<string>? requested = null;
        _installer.InstallAsync(Arg.Do<IReadOnlyCollection<string>?>(ids => requested = ids), Arg.Any<IProgress<PrerequisiteProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MarkAllInstalled()));
        _auth.IsPaired.Returns(true);
        using var vm = Create();

        vm.Steps.Single(s => s.Id == "uv").IsSelected = true;
        await vm.InstallCommand.ExecuteAsync(null);

        requested.ShouldNotBeNull();
        requested.ShouldBe(["uv"]);
    }

    /// <summary>Test: Progress updates step overall percent and log.</summary>
    [Fact]
    public async Task Progress_updates_step_overall_percent_and_log()
    {
        var gate = new TaskCompletionSource<Result>();
        IProgress<PrerequisiteProgress>? progress = null;
        _installer.InstallAsync(Arg.Any<IReadOnlyCollection<string>?>(), Arg.Do<IProgress<PrerequisiteProgress>?>(p => progress = p), Arg.Any<CancellationToken>())
            .Returns(_ => gate.Task);
        using var vm = Create();

        var running = vm.InstallCommand.ExecuteAsync(null);
        vm.Phase.ShouldBe(SetupPhase.Installing);
        vm.IsInstalling.ShouldBeTrue();
        vm.CancelInstallCommand.CanExecute(null).ShouldBeTrue();

        progress.ShouldNotBeNull();
        progress.Report(new PrerequisiteProgress("node", "Node.js", 0, 3, 40, 13, "İndiriliyor", "node-v22.zip 40%"));
        var node = vm.Steps.Single(s => s.Id == "node");
        node.State.ShouldBe(SetupStepState.Running);
        node.Percent.ShouldBe(40);
        node.Detail.ShouldBe("İndiriliyor");
        vm.OverallPercent.ShouldBe(13);
        vm.Logs.ShouldContain(l => l.EndsWith("node-v22.zip 40%", StringComparison.Ordinal));

        progress.Report(new PrerequisiteProgress("chrome-headless-shell", "Chrome", 2, 3, null, 70, "Açılıyor"));
        node.State.ShouldBe(SetupStepState.Done);
        vm.Steps.Single(s => s.Id == "chrome-headless-shell").IsIndeterminate.ShouldBeTrue();

        _auth.IsPaired.Returns(true);
        gate.SetResult(MarkAllInstalled());
        await running;
        vm.Phase.ShouldBe(SetupPhase.Completed);
        vm.OverallPercent.ShouldBe(100);
    }

    /// <summary>Test: Cancel during install returns to retryable failed phase.</summary>
    [Fact]
    public async Task Cancel_during_install_returns_to_retryable_failed_phase()
    {
        _installer.InstallAsync(Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<IProgress<PrerequisiteProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                await Task.Delay(Timeout.Infinite, ci.ArgAt<CancellationToken>(2));
                return Result.Success();
            });
        using var vm = Create();

        var running = vm.InstallCommand.ExecuteAsync(null);
        vm.CancelInstallCommand.Execute(null);
        await running;

        vm.Phase.ShouldBe(SetupPhase.Failed);
        vm.StatusMessage.ShouldContain("iptal");
    }

    /// <summary>Test: Invalid server url blocks install and valid url is saved.</summary>
    [Fact]
    public async Task Invalid_server_url_blocks_install_and_valid_url_is_saved()
    {
        using var vm = Create();
        vm.ServerUrl = "pi-adresi";

        await vm.InstallCommand.ExecuteAsync(null);

        vm.ServerUrlError.ShouldNotBeNull();
        vm.Phase.ShouldBe(SetupPhase.Prerequisites);
        await _installer.DidNotReceiveWithAnyArgs().InstallAsync(default, default, CancellationToken.None);

        _auth.IsPaired.Returns(true);
        SucceedInstall();
        vm.ServerUrl = "http://10.0.0.5:7111/";
        await vm.InstallCommand.ExecuteAsync(null);
        _agentSettings.Current.ServerBaseUrl.ShouldBe("http://10.0.0.5:7111");
        vm.ServerUrlError.ShouldBeNull();
    }

    /// <summary>Test: Skip to pairing only when required components are installed.</summary>
    [Fact]
    public void Skip_to_pairing_only_when_required_components_are_installed()
    {
        _pairing.WaitGate = new TaskCompletionSource();
        using var vm = Create();
        vm.SkipToPairingIfReady().ShouldBeFalse();
        vm.Phase.ShouldBe(SetupPhase.Prerequisites);

        _installed["node"] = true;
        _installed["chrome-headless-shell"] = true;
        using var ready = Create();
        ready.SkipToPairingIfReady().ShouldBeTrue();
        ready.Phase.ShouldBe(SetupPhase.Pairing);
        ready.InstallButtonText.ShouldBe("Devam et");
    }

    /// <summary>Test: Back from pairing returns to component list when idle.</summary>
    [Fact]
    public async Task Back_from_pairing_returns_to_component_list_when_idle()
    {
        _pairing.StartResult = Result.Failure<Shared.Contracts.Agent.Pairing.DeviceStartResponse>(Error.Failure("net", "Ulaşılamadı"));
        SucceedInstall();
        using var vm = Create();
        await vm.InstallCommand.ExecuteAsync(null);
        vm.Phase.ShouldBe(SetupPhase.Pairing);
        vm.Pairing.State.ShouldBe(PairingState.Failed);

        vm.BackToPrerequisitesCommand.CanExecute(null).ShouldBeTrue();
        vm.BackToPrerequisitesCommand.Execute(null);
        vm.Phase.ShouldBe(SetupPhase.Prerequisites);
    }

    /// <summary>Test: Gpu summary describes largest card.</summary>
    [Fact]
    public async Task Gpu_summary_describes_largest_card()
    {
        _capabilities.DetectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AgentCapabilities
        {
            Gpus =
            [
                new GpuInfo { Name = "Intel UHD", VramMb = 512 },
                new GpuInfo { Name = "NVIDIA GeForce RTX 3060 Laptop GPU", VramMb = 6144, CudaAvailable = true },
            ],
        }));
        using var vm = Create();

        await vm.DetectGpuAsync(CancellationToken.None);

        vm.GpuSummary.ShouldBe("NVIDIA GeForce RTX 3060 Laptop GPU · 6 GB VRAM · CUDA hazır");
        SetupViewModel.DescribeGpus([]).ShouldContain("bulunamadı");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        condition().ShouldBeTrue();
    }

    private SetupViewModel Create()
    {
        var pairingVm = new PairingViewModel(_pairing, Substitute.For<IDesktopLauncher>(), new ImmediateUiDispatcher(), NullLogger<PairingViewModel>.Instance);
        return new SetupViewModel(
            _installer,
            _capabilities,
            _agentSettings,
            _desktopSettings,
            _auth,
            new AgentOptions { EnableFileLogging = false },
            pairingVm,
            new ImmediateUiDispatcher(),
            NullLogger<SetupViewModel>.Instance);
    }

    private void SucceedInstall() =>
        _installer.InstallAsync(Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<IProgress<PrerequisiteProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MarkAllInstalled()));

    private Result MarkAllInstalled()
    {
        foreach (var key in _installed.Keys.Where(k => k != "uv").ToList())
        {
            _installed[key] = true;
        }

        return Result.Success();
    }
}
