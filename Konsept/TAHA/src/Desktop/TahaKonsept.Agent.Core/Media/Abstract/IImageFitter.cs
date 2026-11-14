using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Media.Abstract;

/// <summary>
/// Üretilen görseli istenen tam piksel boyutuna getirir. Yerel görsel motorları boyutu modelin hizasına yuvarlayabilir
/// (ör. stable-diffusion.cpp FLUX.2'de 1080 → 1088); sahneler ve sözleşme ise isteğin tam boyutunu bekler.
/// <see cref="FitAsync"/>: boyut zaten doğruysa dokunmaz; değilse oranı koruyarak kaplar ve ortadan kırpar.
/// </summary>
public interface IImageFitter
{
    /// <summary>Görselin piksel boyutunu okur.</summary>
    /// <param name="path">Görsel dosyası.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Genişlik ve yükseklik ya da hata.</returns>
    Task<Result<(int Width, int Height)>> GetSizeAsync(string path, CancellationToken ct);

    /// <summary>Görseli yerinde tam <paramref name="width"/>×<paramref name="height"/> yapar (kapla + ortadan kırp).</summary>
    /// <param name="path">Görsel dosyası (üzerine yazılır).</param>
    /// <param name="width">İstenen genişlik.</param>
    /// <param name="height">İstenen yükseklik.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Değiştirildiyse <c>true</c>, zaten doğru boyuttaysa <c>false</c>; ya da hata.</returns>
    Task<Result<bool>> FitAsync(string path, int width, int height, CancellationToken ct);
}
