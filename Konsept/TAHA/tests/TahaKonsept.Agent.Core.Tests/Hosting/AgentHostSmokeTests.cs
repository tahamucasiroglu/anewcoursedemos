using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.DependencyInjection;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.Core.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Hosting;

/// <summary>Ajan ana makinesi duman testi: eşlenmemişken başlar, video sunucusu cevap verir, hub eşleme bekler, temiz durur.</summary>
public sealed class AgentHostSmokeTests
{
    /// <summary>Başlat → /health 200, hub Unauthorized → durdur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Host_StartsUnpaired_AndStops()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();
        var port = FreePort();
        var services = new ServiceCollection();
        services.AddAgentCore(new AgentOptions { RootDirectory = temp.Path, EnableFileLogging = false, VideoServerPort = port, DefaultServerBaseUrl = "http://127.0.0.1:1" });
        await using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<IAgentHost>();

        var started = await host.StartAsync(ct);

        started.IsSuccess.ShouldBeTrue(started.IsFailure ? started.Error.Message : string.Empty);
        host.IsRunning.ShouldBeTrue();
        using (var client = new HttpClient())
        {
            var health = await client.GetStringAsync(new Uri("http://127.0.0.1:" + port + "/health"), ct);
            health.ShouldBe("ok");
        }

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (host.Hub.Status != HubConnectionStatus.Unauthorized && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, ct);
        }

        host.Hub.Status.ShouldBe(HubConnectionStatus.Unauthorized);
        await host.StopAsync(ct);
        host.IsRunning.ShouldBeFalse();
        host.Hub.Status.ShouldBe(HubConnectionStatus.Disconnected);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
