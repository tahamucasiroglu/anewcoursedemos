using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Aşama girdi/çıktı hash'leri (SHA-256, onaltılık küçük harf). Girdi: aşama adı + sahne + kanonik JSON. Çıktı: çıktı JSON'u (yoksa girdi hash'inden türetilir).
/// </summary>
public interface IStageInputHasher
{
    /// <summary>Girdi hash'i.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="input">Hash girdisi.</param>
    /// <returns>Sonuç.</returns>
    string HashInput(PipelineStage stage, int? sceneIndex, object input);

    /// <summary>Çıktı hash'i.</summary>
    /// <param name="inputHash">Girdi hash'i.</param>
    /// <param name="outputJson">Çıktı JSON'u.</param>
    /// <returns>Sonuç.</returns>
    string HashOutput(string inputHash, string? outputJson);
}
