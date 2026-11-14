using System.Globalization;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Tools;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Gerçek ffmpeg ile test medyası üretir (lavfi testsrc2 videosu, sine sesi).</summary>
public static class MediaFactory
{
    /// <summary>Renkli test videosu üretir.</summary>
    /// <param name="path">Çıktı.</param>
    /// <param name="seconds">Süre.</param>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="fps">Kare hızı.</param>
    /// <returns>Görev.</returns>
    public static Task CreateVideoAsync(string path, double seconds, int width, int height, int fps) =>
        RunAsync(["-y", "-f", "lavfi", "-i", string.Create(CultureInfo.InvariantCulture, $"testsrc2=size={width}x{height}:rate={fps}:duration={seconds}"), "-c:v", "libx264", "-pix_fmt", "yuv420p", path]);

    /// <summary>Sinüs sesi (wav) üretir.</summary>
    /// <param name="path">Çıktı.</param>
    /// <param name="seconds">Süre.</param>
    /// <returns>Görev.</returns>
    public static Task CreateAudioAsync(string path, double seconds) =>
        RunAsync(["-y", "-f", "lavfi", "-i", string.Create(CultureInfo.InvariantCulture, $"sine=frequency=440:duration={seconds}"), path]);

    private static async Task RunAsync(IReadOnlyList<string> args)
    {
        var ffmpeg = new TestToolLocator().Find(ToolKind.Ffmpeg) ?? throw new InvalidOperationException("ffmpeg yok");
        var result = await new TestProcessRunner().RunAsync(new ProcessSpec(ffmpeg, ["-hide_banner", "-loglevel", "error", .. args], Timeout: TimeSpan.FromMinutes(2)), null, CancellationToken.None);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("ffmpeg test medyası üretilemedi: " + result.StandardError);
        }
    }
}
