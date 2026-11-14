namespace TahaKonsept.Agent.Engines.Speech.Devices.Abstract;

/// <summary>
/// Python ses motorları için cihaz seçici ("Önce GPU"): GPU kullanabilen motor, GPU varsa <c>cuda</c>, yoksa nedeniyle birlikte
/// <c>cpu</c> alır. GPU kullanamayan motor (ör. Piper) her zaman tasarım gereği <c>cpu</c>. Önemli metot: <see cref="Select"/>.
/// GPU'suz test edilebilmesi için yoklama somut sınıfta, karar mantığı tabanda.
/// </summary>
public interface ITorchDeviceSelector
{
    /// <summary>Motorun cihazını seçer.</summary>
    /// <param name="supportsGpu">Motor GPU'da çalışabiliyor mu (torch CUDA / ONNX GPU).</param>
    /// <returns>Karar (cihaz + varsa düşüş nedeni).</returns>
    TorchDeviceDecision Select(bool supportsGpu);
}
