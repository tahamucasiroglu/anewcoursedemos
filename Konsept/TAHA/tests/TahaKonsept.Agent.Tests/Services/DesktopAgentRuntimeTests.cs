using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Runtime;
using TahaKonsept.Agent.Runtime.Concrete;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Tests.Fakes;
using TahaKonsept.Agent.Updates.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using Xunit;

namespace TahaKonsept.Agent.Tests.Services;

/// <summary>Uygulama yaşam döngüsü: başlangıç kaydı varsayılanı, pencere kararı, çekirdek başlat/durdur, güncelleme kurulumu.</summary>
public sealed class DesktopAgentRuntimeTests
{
    private readonly IAgentHost _host = Substitute.For<IAgentHost>();
    private readonly IStartupRegistration _startup = Substitute.For<IStartupRegistration>();
    private readonly InMemoryDesktopAppSettingsStore _settings = new();
    private readonly IWindowNavigator _navigator = Substitute.For<IWindowNavigator>();
    private readonly IAgentUpdater _updater = Substitute.For<IAgentUpdater>();

    /// <summary>Sahteleri hazırlar.</summary>
    public DesktopAgentRuntimeTests()
    {
        _host.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _startup.SetEnabled(Arg.Any<bool>()).Returns(Result.Success());
    }

    /// <summary>İlk açılışta Windows ile başlat bir kez açılır, kurulum gerekiyorsa kurulum penceresi gösterilir.</summary>
    [Fact]
    public async Task First_start_enables_startup_once_and_shows_setup()
    {
        _navigator.IsSetupRequired().Returns(true);
        var runtime = Create();

        await runtime.StartAsync(new DesktopLaunchOptions(StartedFromLogin: false), CancellationToken.None);

        _startup.Received(1).SetEnabled(true);
        _settings.Current.StartupDefaultApplied.ShouldBeTrue();
        _navigator.Received(1).ShowSetup(false);
        _navigator.DidNotReceive().ShowStatus();
        await _host.Received(1).StartAsync(Arg.Any<CancellationToken>());

        _startup.ClearReceivedCalls();
        await Create().StartAsync(new DesktopLaunchOptions(false), CancellationToken.None);
        _startup.DidNotReceive().SetEnabled(Arg.Any<bool>());
    }

    /// <summary>Oturum açılışında, kurulum gerekmiyorsa pencere açılmaz; elle açılışta durum penceresi açılır.</summary>
    [Fact]
    public async Task Login_start_stays_in_tray_manual_start_shows_status()
    {
        _navigator.IsSetupRequired().Returns(false);

        await Create().StartAsync(new DesktopLaunchOptions(StartedFromLogin: true), CancellationToken.None);
        _navigator.DidNotReceive().ShowStatus();
        _navigator.DidNotReceive().ShowSetup(Arg.Any<bool>());

        await Create().StartAsync(new DesktopLaunchOptions(StartedFromLogin: false), CancellationToken.None);
        _navigator.Received(1).ShowStatus();
    }

    /// <summary>Durdurma tek seferliktir: pencereler kapanır, çekirdek durur, güncelleme kapanışta kurulur.</summary>
    [Fact]
    public async Task Stop_is_idempotent_and_applies_update()
    {
        var runtime = Create();

        await runtime.StopAsync(CancellationToken.None);
        await runtime.StopAsync(CancellationToken.None);

        _navigator.Received(1).CloseAll();
        await _host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        _updater.Received(1).ApplyOnExit();
    }

    private DesktopAgentRuntime Create() =>
        new(_host, _startup, _settings, _navigator, _updater, new ImmediateUiDispatcher(), NullLogger<DesktopAgentRuntime>.Instance);
}
