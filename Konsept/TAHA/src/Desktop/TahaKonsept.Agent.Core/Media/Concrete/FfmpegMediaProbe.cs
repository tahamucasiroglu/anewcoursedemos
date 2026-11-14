using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Media.Base;

namespace TahaKonsept.Agent.Core.Media.Concrete;

/// <summary>Ajanın kurduğu (ya da PATH'teki) FFmpeg/ffprobe ile çalışan medya yardımcısı.</summary>
public sealed class FfmpegMediaProbe : MediaProbeBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    public FfmpegMediaProbe(IToolLocator tools, IProcessRunner processes)
        : base(tools, processes)
    {
    }
}
