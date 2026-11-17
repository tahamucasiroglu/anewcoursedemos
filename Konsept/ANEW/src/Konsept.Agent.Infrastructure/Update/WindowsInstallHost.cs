using System.Diagnostics;
using Konsept.Agent.Application.Abstract;
using Microsoft.Win32;

namespace Konsept.Agent.Infrastructure.Update;

/// <summary>
/// <see cref="IInstallHost"/>'un Windows tarafı (spec 0023 Req 10): yalnızca <c>HKEY_CURRENT_USER</c> (yönetici izni gerekmez), kullanıcının
/// Başlat menüsü ve kendi süreçleri. Kısayol Windows Komut Dosyası Ana Bilgisayarı'nın (WScript.Shell) COM nesnesiyle PowerShell üzerinden
/// oluşturulur. Windows dışında hiçbir şey yapmaz.
/// </summary>
public sealed class WindowsInstallHost : IInstallHost
{
    /// <inheritdoc />
    public void SetUserValue(string subKey, string name, object value)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var key = Registry.CurrentUser.CreateSubKey(subKey, writable: true);
        key.SetValue(name, value, value is int ? RegistryValueKind.DWord : RegistryValueKind.String);
    }

    /// <inheritdoc />
    public void DeleteUserValue(string subKey, string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }

    /// <inheritdoc />
    public void DeleteUserKey(string subKey)
    {
        if (OperatingSystem.IsWindows())
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
        }
    }

    /// <inheritdoc />
    public void CreateShortcut(string shortcutPath, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        var script = "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('" + Escape(shortcutPath) + "');$s.TargetPath='" + Escape(target)
            + "';$s.WorkingDirectory='" + Escape(Path.GetDirectoryName(target)!) + "';$s.Save()";
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", script })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start);
        process?.WaitForExit(15000);
    }

    /// <inheritdoc />
    public void Launch(string executable, string arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty,
        };
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            start.ArgumentList.Add(arguments);
        }

        using var process = Process.Start(start);
    }

    /// <inheritdoc />
    public void StopRunning(string executable)
    {
        var own = Environment.ProcessId;
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))
        {
            using (process)
            {
                try
                {
                    if (process.Id != own && string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(10000);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Süreç bu arada kapandı ya da erişilemiyor (başka kullanıcınınki): dokunulmaz.
                }
            }
        }
    }

    /// <summary>Kaldırmanın geçici kopyasını, süreç kapandıktan birkaç saniye sonra siler (bir exe kendini silemez).</summary>
    /// <param name="file">Dosya.</param>
    public void DeleteLater(string file)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "/c ping 127.0.0.1 -n 4 > nul & del /f /q \"" + file + "\"",
        };
        using var process = Process.Start(start);
    }

    private static string Escape(string text) => text.Replace("'", "''", StringComparison.Ordinal);
}
