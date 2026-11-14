using System.Diagnostics;
using System.Globalization;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>
/// <c>nvidia-smi</c> ile boş VRAM'i okur. Gerçek donanım testleri, masaüstü uygulamaları (tarayıcı, Steam…) GPU belleğini
/// tuttuğunda CUDA "out of memory" ile düşmek yerine açık bir mesajla atlanabilsin diye kullanılır.
/// </summary>
public static class GpuMemoryProbe
{
    /// <summary>İlk NVIDIA GPU'nun boş belleği (MiB); <c>nvidia-smi</c> yoksa ya da okunamazsa <c>null</c>.</summary>
    /// <returns>Boş MiB ya da <c>null</c>.</returns>
    public static int? FreeMiB()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("nvidia-smi", "--query-gpu=memory.free --format=csv,noheader,nounits")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
            {
                return null;
            }

            var text = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(10_000) || process.ExitCode != 0)
            {
                return null;
            }

            var first = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mib) ? mib : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
