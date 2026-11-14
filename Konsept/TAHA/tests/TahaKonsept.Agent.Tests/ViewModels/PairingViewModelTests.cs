using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Pairing;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Tests.Fakes;
using TahaKonsept.Agent.ViewModels.Concrete;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using Xunit;

namespace TahaKonsept.Agent.Tests.ViewModels;

/// <summary>Eşleme görünüm modeli: kod alma, onay, ret, hata, iptal ve tarayıcı açma.</summary>
public sealed class PairingViewModelTests
{
    private readonly FakePairingService _service = new();
    private readonly IDesktopLauncher _launcher = Substitute.For<IDesktopLauncher>();

    /// <summary>Test: Start then approved sets code and raises paired.</summary>
    [Fact]
    public async Task Start_then_approved_sets_code_and_raises_paired()
    {
        using var vm = Create();
        var paired = 0;
        vm.Paired += (_, _) => paired++;

        await vm.StartCommand.ExecuteAsync(null);

        vm.State.ShouldBe(PairingState.Paired);
        vm.IsPaired.ShouldBeTrue();
        vm.UserCode.ShouldBe("ABCD-EFGH");
        vm.VerificationUrlComplete.ShouldBe(FakePairingService.DefaultStart.VerificationUrlComplete);
        vm.IsBusy.ShouldBeFalse();
        vm.CanRetry.ShouldBeFalse();
        paired.ShouldBe(1);
    }

    /// <summary>Test: While waiting code is shown and browser opens prefilled url.</summary>
    [Fact]
    public async Task While_waiting_code_is_shown_and_browser_opens_prefilled_url()
    {
        _service.WaitGate = new TaskCompletionSource();
        _launcher.OpenUrl(Arg.Any<string>()).Returns(Result.Success());
        using var vm = Create();

        var running = vm.StartCommand.ExecuteAsync(null);

        vm.IsWaiting.ShouldBeTrue();
        vm.HasCode.ShouldBeTrue();
        vm.IsBusy.ShouldBeTrue();
        vm.OpenBrowserCommand.CanExecute(null).ShouldBeTrue();
        vm.OpenBrowserCommand.Execute(null);
        _launcher.Received(1).OpenUrl(FakePairingService.DefaultStart.VerificationUrlComplete);

        _service.WaitGate.SetResult();
        await running;
        vm.IsPaired.ShouldBeTrue();
        vm.HasCode.ShouldBeFalse();
    }

    /// <summary>Test: Denied allows retry and does not raise paired.</summary>
    [Fact]
    public async Task Denied_allows_retry_and_does_not_raise_paired()
    {
        _service.WaitResult = Result.Failure(Error.Failure(DeviceTokenErrorCodes.AccessDenied, "Reddedildi"));
        using var vm = Create();
        var paired = false;
        vm.Paired += (_, _) => paired = true;

        await vm.StartCommand.ExecuteAsync(null);

        vm.State.ShouldBe(PairingState.Denied);
        vm.CanRetry.ShouldBeTrue();
        vm.StartButtonText.ShouldBe("Yeni kod al");
        paired.ShouldBeFalse();
    }

    /// <summary>Test: Start failure shows message and skips waiting.</summary>
    [Fact]
    public async Task Start_failure_shows_message_and_skips_waiting()
    {
        _service.StartResult = Result.Failure<DeviceStartResponse>(Error.Failure("net", "Sunucuya ulaşılamadı"));
        using var vm = Create();

        await vm.StartCommand.ExecuteAsync(null);

        vm.State.ShouldBe(PairingState.Failed);
        vm.Message.ShouldContain("Sunucuya ulaşılamadı");
        vm.HasCode.ShouldBeFalse();
        vm.CanRetry.ShouldBeTrue();
    }

    /// <summary>Test: Cancel stops waiting.</summary>
    [Fact]
    public async Task Cancel_stops_waiting()
    {
        _service.WaitGate = new TaskCompletionSource();
        using var vm = Create();

        var running = vm.StartCommand.ExecuteAsync(null);
        vm.CancelCommand.CanExecute(null).ShouldBeTrue();
        vm.CancelCommand.Execute(null);
        await running;

        vm.State.ShouldBe(PairingState.Cancelled);
        vm.IsBusy.ShouldBeFalse();
        vm.CanRetry.ShouldBeTrue();
    }

    /// <summary>Test: Retry after failure requests new code.</summary>
    [Fact]
    public async Task Retry_after_failure_requests_new_code()
    {
        _service.WaitResult = Result.Failure(Error.Failure(DeviceTokenErrorCodes.ExpiredToken, "Süre doldu"));
        using var vm = Create();
        await vm.StartCommand.ExecuteAsync(null);

        _service.WaitResult = Result.Success();
        await vm.StartCommand.ExecuteAsync(null);

        _service.StartCalls.ShouldBe(2);
        vm.IsPaired.ShouldBeTrue();
    }

    private PairingViewModel Create() =>
        new(_service, _launcher, new ImmediateUiDispatcher(), NullLogger<PairingViewModel>.Instance);
}
