namespace TahaKonsept.Agent.Abstractions.Paths.Abstract;

/// <summary>
/// Ajanın tüm klasörleri. Kök: <c>%LocalAppData%/TahaKonsept</c> (ad çakışmasını önlemek için ürün adına özel).
/// </summary>
public interface IAgentPaths
{
    /// <summary>Kök klasör.</summary>
    string RootDir { get; }

    /// <summary>Portable araçlar (node, ffmpeg, uv, sd.cpp…).</summary>
    string ToolsDir { get; }

    /// <summary>Model dosyaları (onnx, gguf, safetensors).</summary>
    string ModelsDir { get; }

    /// <summary>Python sanal ortamları (motor başına).</summary>
    string PythonEnvsDir { get; }

    /// <summary>İş çalışma alanları.</summary>
    string WorkspaceDir { get; }

    /// <summary>Son videolar.</summary>
    string VideosDir { get; }

    /// <summary>Yerel loglar (7 gün).</summary>
    string LogsDir { get; }

    /// <summary>Ayarlar ve şifreli token dosyaları.</summary>
    string ConfigDir { get; }

    /// <summary>Bir işin çalışma klasörü (yoksa oluşturur).</summary>
    /// <param name="jobId">İş kimliği.</param>
    /// <returns>Klasör yolu.</returns>
    string GetJobDir(Guid jobId);
}
