using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Yerel model çalışma ortamının Windows tarafı (spec 0028 AC-4, AC-6): tar.gz açma (taşınabilir Python), uv kurulum sürecinin argümanları ve
/// yalıtılmış ortamı, kilit hash'i uyuşmazsa uv hiç çalışmaz, gerçek süreç çalıştırıcıda süre aşımı ve Durdur süreç ağacını öldürür.
/// </summary>
public sealed class ModelRuntimeHostTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-runtime-");

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>tar.gz arşivi (python-build-standalone biçimi) kök klasörüyle açılır.</summary>
    [Fact]
    public void ExtractArchive_TarGz()
    {
        var archive = Path.Combine(_root.FullName, "py.tar.gz");
        var source = Path.Combine(_root.FullName, "src", "python");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "python.exe"), "py");
        using (var file = File.Create(archive))
        using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
        {
            TarFile.CreateFromDirectory(Path.Combine(_root.FullName, "src"), gzip, includeBaseDirectory: false);
        }

        new WindowsSetupHost(new HttpClient()).ExtractArchive(archive, Path.Combine(_root.FullName, "out"));

        File.ReadAllText(Path.Combine(_root.FullName, "out", "python", "python.exe")).ShouldBe("py");
    }

    /// <summary>
    /// AC-4: uv yalnızca hash'li kilitle, bağımlılık çözmeden, hedef klasöre, verilen Python'la kurar; kullanıcının UV_/PIP_/PYTHONPATH değişkenleri
    /// silinir, yapılandırma dosyası ve Python indirmesi kapalı, önbellek ajan klasöründe.
    /// </summary>
    [Fact]
    public void UvStartInfo_HashedTargetIsolated()
    {
        Environment.SetEnvironmentVariable("UV_INDEX_URL", "https://evil.invalid/simple");
        Environment.SetEnvironmentVariable("PIP_EXTRA_INDEX_URL", "https://evil.invalid/simple");
        try
        {
            var info = WindowsSetupHost.CreateUvStartInfo(@"C:\a\uv.exe", @"C:\a\python.exe", @"C:\c\requirements.txt", @"C:\a\speech.partial", @"C:\c");

            info.ArgumentList.ShouldBe([
                "pip", "install", "--no-config", "--require-hashes", "--no-deps", "--index-strategy", "unsafe-best-match", "--link-mode", "copy",
                "--python", @"C:\a\python.exe", "--target", @"C:\a\speech.partial", "-r", @"C:\c\requirements.txt"]);
            info.Environment.ContainsKey("UV_INDEX_URL").ShouldBeFalse();
            info.Environment.ContainsKey("PIP_EXTRA_INDEX_URL").ShouldBeFalse();
            (info.Environment["UV_CACHE_DIR"], info.Environment["UV_NO_CONFIG"], info.Environment["UV_PYTHON_DOWNLOADS"]).ShouldBe((@"C:\c", "1", "never"));
            (info.CreateNoWindow, info.UseShellExecute).ShouldBe((true, false));
        }
        finally
        {
            Environment.SetEnvironmentVariable("UV_INDEX_URL", null);
            Environment.SetEnvironmentVariable("PIP_EXTRA_INDEX_URL", null);
        }
    }

    /// <summary>AC-4: kilit hash'i uyuşmazsa uv hiç çalıştırılmaz (sahte uv yolu bile denenmez), önbellek bırakılmaz; bilinmeyen ortam kurulmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task PythonInstall_LockMismatchOrUnknown_False()
    {
        var host = new WindowsSetupHost(new HttpClient());
        var cache = Path.Combine(_root.FullName, "cache");

        (await host.InstallPythonPackagesAsync("speech-onnx", null, new string('0', 64), "yok-uv.exe", "yok-python.exe", _root.FullName, cache, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await host.InstallPythonPackagesAsync("bilinmeyen", null, new string('0', 64), "yok-uv.exe", "yok-python.exe", _root.FullName, cache, TestContext.Current.CancellationToken)).ShouldBeFalse();
        Directory.Exists(cache).ShouldBeFalse();
    }

    /// <summary>AC-6, AC-10: süre aşımında süreç öldürülür (TimedOut), Durdur'da OperationCanceledException; çıktı ve çıkış kodu okunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProcessRunner_TimeoutCancelOutput()
    {
        var runner = new ProcessRunner();
        var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var env = new Dictionary<string, string> { ["KONSEPT_TEST"] = "değer" };

        var echo = await runner.RunAsync(new ProcessSpec(cmd, ["/c", "echo %KONSEPT_TEST% & exit 3"], _root.FullName, env, TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken);
        echo.ExitCode.ShouldBe(3);
        echo.Ok.ShouldBeFalse();

        var watch = Stopwatch.StartNew();
        var slow = new ProcessSpec(cmd, ["/c", "ping -n 30 127.0.0.1 >nul"], _root.FullName, env, TimeSpan.FromMilliseconds(500));
        (await runner.RunAsync(slow, TestContext.Current.CancellationToken)).TimedOut.ShouldBeTrue();
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(15));

        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(slow with { Timeout = TimeSpan.FromMinutes(1) }, stop.Token));

        (await runner.RunAsync(new ProcessSpec(Path.Combine(_root.FullName, "yok.exe"), [], _root.FullName, env, TimeSpan.FromSeconds(5)), TestContext.Current.CancellationToken)).ExitCode.ShouldBe(-1);
    }
}
