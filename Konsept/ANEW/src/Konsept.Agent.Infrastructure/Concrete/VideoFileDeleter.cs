using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>Dosya sisteminden kalıcı silme (spec 0019 Req 4); açık ya da izinsiz dosyada <see cref="File.Delete(string)"/> hata verir → "kalan".</summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
public sealed class VideoFileDeleter(Func<string> videoFolder) : VideoFileDeleterBase(videoFolder)
{
    /// <inheritdoc />
    protected override void DeleteFile(string path) => File.Delete(path);
}
