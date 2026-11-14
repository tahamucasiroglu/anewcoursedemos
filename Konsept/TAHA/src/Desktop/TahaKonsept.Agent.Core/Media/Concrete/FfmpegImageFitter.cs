using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Media.Base;

namespace TahaKonsept.Agent.Core.Media.Concrete;

/// <summary>Ajanın kurduğu (ya da PATH'teki) ffprobe/ffmpeg ile görseli istenen tam boyuta getirir.</summary>
public sealed class FfmpegImageFitter : ImageFitterBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    public FfmpegImageFitter(IToolLocator tools, IProcessRunner processes)
        : base(tools, processes)
    {
    }
}
