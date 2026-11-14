using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Speech.Devices.Abstract;
using TahaKonsept.Agent.Engines.Speech.Onnx.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Base;

/// <summary>
/// Ses motorlarının ortak bağımlılıkları (tek parametreyle taşınır, somut motor kurucuları kısa kalır):
/// klasörler, araç bulucu, araç kurucu, süreç çalıştırıcı, HTTP istemci fabrikası (<see cref="HttpClientName"/>), log fabrikası,
/// Python motorları için cihaz seçici (<see cref="DeviceSelector"/>, "Önce GPU") ve ONNX oturum fabrikası (<see cref="OnnxSessions"/>, DirectML → CPU).
/// </summary>
/// <param name="Paths">Ajan klasörleri (ModelsDir, PythonEnvsDir, ToolsDir).</param>
/// <param name="Tools">uv / python yolunu bulan araç bulucu.</param>
/// <param name="Processes">Kısa süreli komutlar (uv venv, uv pip install) için süreç çalıştırıcı.</param>
/// <param name="HttpClientFactory">Model indirmeleri için HTTP istemci fabrikası.</param>
/// <param name="LoggerFactory">Log fabrikası.</param>
/// <param name="Provisioner">Eksik aracı (uv) gerektiğinde kuran servis; <c>null</c> ise eksik araç anlaşılır hatayla sonuçlanır.</param>
/// <param name="DeviceSelector">Python motorlarının cihaz seçicisi; <c>null</c> ise GPU kullanabilen motor <c>cuda</c> ister (adapter GPU göremezse uyarıyla CPU'ya düşer).</param>
/// <param name="OnnxSessions">Supertonic ONNX oturum fabrikası (DirectML → CPU); <c>null</c> ise motor varsayılan DirectML fabrikasını kullanır.</param>
public sealed record SpeechEngineServices(
    IAgentPaths Paths,
    IToolLocator Tools,
    IProcessRunner Processes,
    IHttpClientFactory HttpClientFactory,
    ILoggerFactory LoggerFactory,
    IToolProvisioner? Provisioner = null,
    ITorchDeviceSelector? DeviceSelector = null,
    IOnnxSessionFactory? OnnxSessions = null)
{
    /// <summary>Model indirmeleri için adlandırılmış HTTP istemcisi.</summary>
    public const string HttpClientName = "TahaKonsept.Speech.Downloads";
}
