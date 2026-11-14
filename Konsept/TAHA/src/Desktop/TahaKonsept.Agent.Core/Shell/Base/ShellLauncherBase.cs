using TahaKonsept.Agent.Core.Shell.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Shell.Base;

/// <summary>
/// Kabuk başlatıcı tabanı: dosya varlığını denetler, başlatma hatalarını <see cref="Result"/>'a çevirir.
/// Platforma özgü başlatma <see cref="LaunchFolder"/> ve <see cref="LaunchFile"/> ile türetilen sınıftadır.
/// </summary>
public abstract class ShellLauncherBase : IShellLauncher
{
    /// <inheritdoc />
    public Result OpenFolderAndSelect(string filePath) => Run(filePath, LaunchFolder);

    /// <inheritdoc />
    public Result OpenWithDefaultApp(string filePath) => Run(filePath, LaunchFile);

    /// <summary>Klasörü açıp dosyayı seçer.</summary>
    /// <param name="fullPath">Tam yol.</param>
    protected abstract void LaunchFolder(string fullPath);

    /// <summary>Dosyayı varsayılan uygulamayla açar.</summary>
    /// <param name="fullPath">Tam yol.</param>
    protected abstract void LaunchFile(string fullPath);

    private static Result Run(string filePath, Action<string> launch)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return Error.NotFound("agent.file_missing", "Dosya bulunamadı: " + filePath);
        }

        try
        {
            launch(Path.GetFullPath(filePath));
            return Result.Success();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            return Error.Failure("agent.shell_failed", "Açılamadı: " + ex.Message);
        }
    }
}
