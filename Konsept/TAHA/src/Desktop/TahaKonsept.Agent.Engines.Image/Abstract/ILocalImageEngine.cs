using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.Abstract;

/// <summary>
/// Model bazında kurulum bilgisi veren yerel görsel motoru. <see cref="IImageEngine"/>'e ek olarak
/// tek bir modelin dosyalarının hazır olup olmadığını söyler ve eksikse indirir (kurulum ekranı için).
/// </summary>
public interface ILocalImageEngine : IImageEngine
{
    /// <summary>Modelin tüm dosyaları hazır mı (ağ kullanmadan).</summary>
    /// <param name="modelKey">Model anahtarı.</param>
    /// <returns>Hazırsa <c>true</c>.</returns>
    bool IsModelInstalled(string modelKey);

    /// <summary>Modelin eksik dosyalarını indirir (kaldığı yerden devam eder).</summary>
    /// <param name="modelKey">Model anahtarı.</param>
    /// <param name="progress">İlerleme (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    Task<Result> EnsureModelAsync(string modelKey, IProgress<EngineSetupProgress>? progress, CancellationToken ct);
}
