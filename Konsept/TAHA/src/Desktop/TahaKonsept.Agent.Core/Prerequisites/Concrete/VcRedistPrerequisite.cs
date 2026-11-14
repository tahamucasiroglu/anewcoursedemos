using System.Diagnostics;
using Microsoft.Win32;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// Microsoft Visual C++ 2015–2022 Redistributable (x64). Algılama kayıt defterinden
/// (<c>HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64</c> <c>Installed=1</c>); kurulum isteğe bağlıdır:
/// <c>vc_redist.x64.exe /install /quiet /norestart</c> yönetici onayıyla (UAC) çalışır. 0, 1638, 3010 çıkış kodları başarıdır.
/// </summary>
public sealed class VcRedistPrerequisite : PrerequisiteBase
{
    /// <summary>İndirme adresi.</summary>
    public static readonly Uri DownloadUrl = new("https://aka.ms/vs/17/release/vc_redist.x64.exe");

    private const string RuntimeKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";
    private const string RuntimeKeyWow = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";

    private readonly IAgentPaths _paths;
    private readonly IFileDownloader _downloader;

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    public VcRedistPrerequisite(IAgentPaths paths, IFileDownloader downloader)
    {
        _paths = paths;
        _downloader = downloader;
    }

    /// <inheritdoc />
    public override string Id => "vc-redist";

    /// <inheritdoc />
    public override string DisplayName => "Visual C++ Yeniden Dağıtılabilir (x64)";

    /// <inheritdoc />
    public override string Description => "Bazı yerel motorların (ONNX, stable-diffusion.cpp) ihtiyaç duyduğu Microsoft çalışma zamanı. Kurulum yönetici onayı ister.";

    /// <inheritdoc />
    public override bool IsOptional => true;

    /// <inheritdoc />
    public override int Order => 5;

    /// <inheritdoc />
    public override string? InstalledVersion => ReadValue("Version") as string;

    /// <inheritdoc />
    public override bool IsInstalled() => ReadValue("Installed") is int installed && installed == 1;

    /// <inheritdoc />
    public override async Task<Result> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (IsInstalled())
        {
            Report(progress, DisplayName + " zaten kurulu.", 100);
            return Result.Success();
        }

        if (!OperatingSystem.IsWindows())
        {
            return Error.Failure("agent.prerequisite_unsupported", "Visual C++ Redistributable yalnızca Windows'ta kurulur.");
        }

        var installer = Path.Combine(_paths.ToolsDir, "_downloads", "vc_redist.x64.exe");
        var downloadProgress = new InlineProgress<double>(p => Report(progress, DisplayName + " indiriliyor… %" + Math.Round(p), p * 0.7));
        var downloaded = await _downloader.DownloadAsync(DownloadUrl, installer, null, downloadProgress, ct);
        if (downloaded.IsFailure)
        {
            return downloaded.Error;
        }

        Report(progress, DisplayName + " kuruluyor (yönetici onayı gerekebilir)…", 75);
        try
        {
            var exitCode = await RunElevatedAsync(installer, ct);
            return exitCode is 0 or 1638 or 3010
                ? Result.Success()
                : Error.Failure("agent.prerequisite_install_failed", DisplayName + " kurulamadı (çıkış kodu " + exitCode + ").");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return Error.Failure("agent.prerequisite_install_cancelled", DisplayName + " kurulumu onaylanmadı: " + ex.Message);
        }
        finally
        {
            try
            {
                File.Delete(installer);
            }
            catch (IOException)
            {
                // Sonra temizlenir.
            }
        }
    }

    private static async Task<int> RunElevatedAsync(string installer, CancellationToken ct)
    {
        var info = new ProcessStartInfo(installer, "/install /quiet /norestart") { UseShellExecute = true, Verb = "runas" };
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Kurulum başlatılamadı.");
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    private static object? ReadValue(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RuntimeKey) ?? Registry.LocalMachine.OpenSubKey(RuntimeKeyWow);
            return key?.GetValue(name);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }
}
