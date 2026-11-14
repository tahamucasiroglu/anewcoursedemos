using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// LLM çıktısını sahne koduna çevirir ve motora göre biçimini doğrular (kod çiti temizlenir, zorunlu işaretler aranır).
/// </summary>
public interface ISceneCodeValidator
{
    /// <summary>Temiz sahne kodu ya da doğrulama hatası.</summary>
    /// <param name="engineKey">Motor.</param>
    /// <param name="rawText">LLM metni.</param>
    /// <returns>Sonuç.</returns>
    Result<string> Extract(string engineKey, string rawText);
}
