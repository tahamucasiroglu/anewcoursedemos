using Konsept.Agent.Infrastructure.Base;
using Microsoft.Win32;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Windows ekran kartı tespiti (spec 0028 Req 5): görüntü bağdaştırıcısı sınıfının kayıt defteri anahtarlarından (<c>DriverDesc</c>,
/// <c>ProviderName</c>, 64 bit <c>HardwareInformation.qwMemorySize</c>; yoksa 32 bit <c>HardwareInformation.MemorySize</c>) okur. Yalnızca okur,
/// yönetici izni gerekmez.
/// </summary>
public sealed class WindowsGpuProbe : GpuProbeBase
{
    private const string DisplayClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    /// <inheritdoc />
    protected override IEnumerable<(string Name, string Provider, long VramBytes)> ReadAdapters()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        using var root = Registry.LocalMachine.OpenSubKey(DisplayClass);
        if (root is null)
        {
            yield break;
        }

        foreach (var name in root.GetSubKeyNames().Where(n => n.Length == 4 && n.All(char.IsAsciiDigit)))
        {
            (string, string, long)? adapter = null;
            try
            {
                using var key = root.OpenSubKey(name);
                if (key is not null)
                {
                    adapter = (key.GetValue("DriverDesc") as string ?? string.Empty, key.GetValue("ProviderName") as string ?? string.Empty, Memory(key));
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // Okunamayan bağdaştırıcı atlanır.
            }

            if (adapter is { } found)
            {
                yield return found;
            }
        }
    }

    private static long Memory(RegistryKey key)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 0;
        }

        return key.GetValue("HardwareInformation.qwMemorySize") switch
        {
            long q => q,
            byte[] { Length: 8 } b => BitConverter.ToInt64(b),
            _ => key.GetValue("HardwareInformation.MemorySize") switch
            {
                int d => (uint)d,
                byte[] { Length: >= 4 } b => BitConverter.ToUInt32(b),
                _ => 0,
            },
        };
    }
}
