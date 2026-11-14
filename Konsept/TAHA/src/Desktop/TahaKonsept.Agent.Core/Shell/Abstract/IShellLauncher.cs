using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Shell.Abstract;

/// <summary>İşletim sistemi kabuğu işlemleri: dosyayı Gezgin'de seçili göster, varsayılan uygulamayla aç.</summary>
public interface IShellLauncher
{
    /// <summary>Dosyanın klasörünü açar ve dosyayı seçer.</summary>
    /// <param name="filePath">Dosya.</param>
    /// <returns>Sonuç.</returns>
    Result OpenFolderAndSelect(string filePath);

    /// <summary>Dosyayı varsayılan uygulamayla açar (ör. sistem video oynatıcısı).</summary>
    /// <param name="filePath">Dosya.</param>
    /// <returns>Sonuç.</returns>
    Result OpenWithDefaultApp(string filePath);
}
