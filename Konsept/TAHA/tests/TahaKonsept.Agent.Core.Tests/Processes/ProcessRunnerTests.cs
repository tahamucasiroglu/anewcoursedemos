using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Core.Processes.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Processes;

/// <summary>
/// <see cref="ProcessRunner"/> testleri (Windows <c>cmd.exe</c> ile): satır akışı, çıkış kodu, stdin, ortam değişkeni,
/// zaman aşımında ve iptalde süreç ağacının öldürülmesi.
/// </summary>
public sealed class ProcessRunnerTests
{
    private readonly ProcessRunner _runner = new(NullLogger<ProcessRunner>.Instance);

    /// <summary>Stdout satırları geri çağrıya ve sonuca gelir; çıkış kodu 0.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_StreamsOutputLines_AndReturnsExitCode()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");
        var lines = new List<string>();

        var result = await _runner.RunAsync(new ProcessSpec("cmd.exe", ["/c", "echo", "merhaba"]), lines.Add, TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        result.ExitCode.ShouldBe(0);
        lines.ShouldContain(l => l.Trim() == "merhaba");
        result.StandardOutput.ShouldContain("merhaba");
    }

    /// <summary>stdout ve stderr'e aynı anda yoğun yazan süreçte geri çağrılar eşzamanlı çalışmaz (kilitsiz tampon bozulmaz, satır kaybolmaz).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_SerializesCallbacksAcrossStreams()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");
        var inside = 0;
        var overlaps = 0;
        var count = 0;

        var result = await _runner.RunAsync(
            new ProcessSpec("cmd.exe", ["/c", "for /L %i in (1,1,300) do @(echo o%i & echo e%i 1>&2)"]),
            _ =>
            {
                if (Interlocked.Increment(ref inside) > 1)
                {
                    Interlocked.Increment(ref overlaps);
                }

                Thread.SpinWait(2000);
                count++;
                Interlocked.Decrement(ref inside);
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        overlaps.ShouldBe(0);
        count.ShouldBe(600);
    }

    /// <summary>Sıfır olmayan çıkış kodu döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_ReturnsNonZeroExitCode()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");

        var result = await _runner.RunAsync(new ProcessSpec("cmd.exe", ["/c", "exit", "3"]), null, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(3);
        result.Succeeded.ShouldBeFalse();
        result.TimedOut.ShouldBeFalse();
    }

    /// <summary>Stdin yazılır ve kapatılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_WritesStandardInput()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");

        var result = await _runner.RunAsync(
            new ProcessSpec("cmd.exe", ["/c", "sort"], StandardInput: "zeytin\r\nelma\r\n"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        lines.ShouldBe(["elma", "zeytin"]);
    }

    /// <summary>Ek ortam değişkenleri ve çalışma klasörü uygulanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_AppliesEnvironmentAndWorkingDirectory()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");
        var workDir = Path.GetTempPath();

        var env = await _runner.RunAsync(
            new ProcessSpec("cmd.exe", ["/c", "echo", "%TK_TEST_VAR%"], Environment: new Dictionary<string, string> { ["TK_TEST_VAR"] = "deger42" }),
            null,
            TestContext.Current.CancellationToken);
        var cwd = await _runner.RunAsync(new ProcessSpec("cmd.exe", ["/c", "cd"], WorkingDirectory: workDir), null, TestContext.Current.CancellationToken);

        env.StandardOutput.Trim().ShouldBe("deger42");
        Path.GetFullPath(cwd.StandardOutput.Trim()).TrimEnd('\\').ShouldBe(Path.GetFullPath(workDir).TrimEnd('\\'), StringCompareShould.IgnoreCase);
    }

    /// <summary>Zaman aşımında süreç ağacı öldürülür ve <see cref="ProcessResult.TimedOut"/> döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_TimesOut_AndKillsTree()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");

        var result = await _runner.RunAsync(
            new ProcessSpec("cmd.exe", ["/c", "ping", "-n", "30", "127.0.0.1"], Timeout: TimeSpan.FromMilliseconds(700)),
            null,
            TestContext.Current.CancellationToken);

        result.TimedOut.ShouldBeTrue();
        result.Succeeded.ShouldBeFalse();
        result.Duration.ShouldBeLessThan(TimeSpan.FromSeconds(15));
    }

    /// <summary>İptalde süreç öldürülür ve <see cref="OperationCanceledException"/> fırlatılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Run_Cancelled_ThrowsAndKills()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Yalnızca Windows.");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));
        var started = DateTime.UtcNow;

        await Should.ThrowAsync<OperationCanceledException>(() =>
            _runner.RunAsync(new ProcessSpec("cmd.exe", ["/c", "ping", "-n", "30", "127.0.0.1"]), null, cts.Token));

        (DateTime.UtcNow - started).ShouldBeLessThan(TimeSpan.FromSeconds(15));
    }
}
