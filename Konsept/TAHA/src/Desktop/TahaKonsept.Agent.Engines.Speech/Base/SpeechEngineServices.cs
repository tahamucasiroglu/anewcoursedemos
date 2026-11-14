using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Base;

/// <summary>
/// Ses motorlarının ortak bağımlılıkları (tek parametreyle taşınır, somut motor kurucuları kısa kalır):
/// klasörler, araç bulucu, süreç çalıştırıcı, HTTP istemci fabrikası (<see cref="HttpClientName"/>) ve log fabrikası.
/// </summary>
/// <param name="Paths">Ajan klasörleri (ModelsDir, PythonEnvsDir, ToolsDir).</param>
/// <param name="Tools">uv / python yolunu bulan araç bulucu.</param>
/// <param name="Processes">Kısa süreli komutlar (uv venv, uv pip install) için süreç çalıştırıcı.</param>
/// <param name="HttpClientFactory">Model indirmeleri için HTTP istemci fabrikası.</param>
/// <param name="LoggerFactory">Log fabrikası.</param>
public sealed record SpeechEngineServices(
    IAgentPaths Paths,
    IToolLocator Tools,
    IProcessRunner Processes,
    IHttpClientFactory HttpClientFactory,
    ILoggerFactory LoggerFactory)
{
    /// <summary>Model indirmeleri için adlandırılmış HTTP istemcisi.</summary>
    public const string HttpClientName = "TahaKonsept.Speech.Downloads";
}
