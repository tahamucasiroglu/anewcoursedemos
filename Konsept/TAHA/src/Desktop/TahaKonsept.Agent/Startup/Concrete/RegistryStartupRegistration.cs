using Microsoft.Win32;
using TahaKonsept.Agent.Startup.Base;

namespace TahaKonsept.Agent.Startup.Concrete;

/// <summary>
/// Windows başlangıç kaydı: <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> altında <c>TahaKonsept.Agent</c> değeri
/// (yönetici izni gerekmez, yalnızca bu kullanıcı için).
/// </summary>
public sealed class RegistryStartupRegistration : StartupRegistrationBase
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <inheritdoc />
    protected override string? ReadValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string;
    }

    /// <inheritdoc />
    protected override void WriteValue(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    /// <inheritdoc />
    protected override void DeleteValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
