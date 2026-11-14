using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Headless;
using TahaKonsept.Agent.Headless.Concrete;
using TahaKonsept.Agent.Tests.Fakes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using Xunit;

namespace TahaKonsept.Agent.Tests.Services;

/// <summary>Penceresiz mod (<c>--headless</c>): argüman çözümü; adres kaydı, eşleme kodunun dosyaya yazılması, kapanışta çekirdeğin durması.</summary>
public sealed class HeadlessAgentRunnerTests
{
    /// <summary>Argümanlar çözülür.</summary>
    [Fact]
    public void Parse_reads_arguments()
    {
        string[] args = ["--headless", "--server", "http://localhost:7191/", "--pair-code-out", "C:\\tmp\\pair.txt", "--stop-file", "C:\\tmp\\stop", "--skip-prerequisites"];

        HeadlessLaunchOptions.IsHeadless(args).ShouldBeTrue();
        HeadlessLaunchOptions.IsHeadless(["--tray"]).ShouldBeFalse();
        var options = HeadlessLaunchOptions.Parse(args);
        options.ServerUrl.ShouldBe("http://localhost:7191");
        options.PairCodeOut.ShouldBe("C:\\tmp\\pair.txt");
        options.StopFile.ShouldBe("C:\\tmp\\stop");
        options.SkipPrerequisites.ShouldBeTrue();
    }

    /// <summary>Eşlenmemiş ajan: adres kaydedilir, kod dosyaya yazılır, onaydan sonra PAIRED; iptalde çekirdek durur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairs_writes_code_and_stops_on_cancel()
    {
        var file = Path.Combine(Path.GetTempPath(), "tk-headless-" + Guid.NewGuid().ToString("N") + ".txt");
        var pairing = new FakePairingService();
        var prerequisites = Substitute.For<IPrerequisiteInstaller>();
        prerequisites.AreRequiredInstalled().Returns(true);
        var host = Substitute.For<IAgentHost>();
        host.Pairing.Returns(pairing);
        host.Prerequisites.Returns(prerequisites);
        host.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var auth = Substitute.For<IAgentAuthService>();
        var paired = false;
        auth.IsPaired.Returns(_ => paired);
        var settings = new InMemoryAgentSettingsStore();
        using var cts = new CancellationTokenSource();
        pairing.StatusChanged += (_, s) =>
        {
            if (s.State == Core.Pairing.PairingState.Paired)
            {
                paired = true;
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));
            }
        };

        var runner = new HeadlessAgentRunner(host, auth, settings, NullLogger<HeadlessAgentRunner>.Instance);
        var code = await runner.RunAsync(new HeadlessLaunchOptions("http://localhost:7191", file, false), cts.Token);

        code.ShouldBe(0);
        settings.Current.ServerBaseUrl.ShouldBe("http://localhost:7191");
        pairing.StartCalls.ShouldBe(1);
        File.ReadAllText(file).ShouldStartWith("PAIRED");
        await host.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        File.Delete(file);
    }
}
