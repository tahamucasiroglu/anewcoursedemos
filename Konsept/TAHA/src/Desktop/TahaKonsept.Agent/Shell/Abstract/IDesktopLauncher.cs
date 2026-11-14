using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Shell.Abstract;

/// <summary>Varsayılan tarayıcıda adres açma ve Gezgin'de klasör açma (tepsi menüsü ve pencereler kullanır).</summary>
public interface IDesktopLauncher
{
    /// <summary>Adresi varsayılan tarayıcıda açar (yalnızca http/https).</summary>
    /// <param name="url">Adres.</param>
    /// <returns>Sonuç.</returns>
    Result OpenUrl(string url);

    /// <summary>Klasörü Gezgin'de açar (yoksa oluşturur).</summary>
    /// <param name="path">Klasör yolu.</param>
    /// <returns>Sonuç.</returns>
    Result OpenFolder(string path);
}
