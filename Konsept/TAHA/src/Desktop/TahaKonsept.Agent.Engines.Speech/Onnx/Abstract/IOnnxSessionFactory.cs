using Microsoft.ML.OnnxRuntime;

namespace TahaKonsept.Agent.Engines.Speech.Onnx.Abstract;

/// <summary>
/// ONNX oturumlarını "Önce GPU" kuralıyla açar: GPU isteniyorsa önce GPU sağlayıcısı (DirectML) ile dener; sağlayıcı eklenemez ya da
/// oturumlar açılamazsa CPU ile yeniden açar ve düşüşü <c>Warning</c> olarak loglar (sessiz düşüş yok). Önemli metot: <see cref="Load{T}"/>.
/// <see cref="GpuAdapterName"/>: GPU sağlayıcısının seçtiği ekran kartının adı (loglarda RTX mi tümleşik GPU mu görünsün diye).
/// </summary>
public interface IOnnxSessionFactory
{
    /// <summary>GPU sağlayıcısının kullandığı ekran kartının adı (ör. "NVIDIA GeForce RTX 3060 Laptop GPU"); bilinmiyorsa <c>null</c>.</summary>
    string? GpuAdapterName { get; }

    /// <summary>Sağlayıcı sırasına göre (GPU → CPU) <paramref name="open"/> ile oturum(lar)ı açar.</summary>
    /// <typeparam name="T">Açılan nesne (bir ya da birden çok <see cref="InferenceSession"/> tutan).</typeparam>
    /// <param name="preferGpu">GPU sağlayıcısı denensin mi.</param>
    /// <param name="open">Verilen <see cref="SessionOptions"/> ile oturum(lar)ı açan işlev; hata verirse kendi açtıklarını kapatmalıdır.</param>
    /// <returns>Açılan nesne, kullanılan sağlayıcı ve varsa düşüş nedeni.</returns>
    OnnxLoadResult<T> Load<T>(bool preferGpu, Func<SessionOptions, T> open);
}
