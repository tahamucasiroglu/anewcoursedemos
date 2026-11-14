namespace Konsept.Shared.Logging.Abstract;

/// <summary>Yerel log dosyalarının saklama süresini uygular (spec 0008 Req 4).</summary>
public interface ILogFileJanitor
{
    /// <summary>Klasördeki, saklama süresinden eski log dosyalarını siler.</summary>
    /// <param name="directory">Log klasörü.</param>
    /// <returns>Silinen dosya sayısı.</returns>
    int Clean(string directory);
}
