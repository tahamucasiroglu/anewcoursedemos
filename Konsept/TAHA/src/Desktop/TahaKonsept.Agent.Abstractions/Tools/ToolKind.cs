namespace TahaKonsept.Agent.Abstractions.Tools;

/// <summary>Ajanın kullandığı harici araç türleri. <c>IToolLocator</c> bunların yolunu bulur.</summary>
public enum ToolKind
{
    /// <summary>node.exe (portable).</summary>
    Node,

    /// <summary>npm (cli js).</summary>
    Npm,

    /// <summary>npx (cli js).</summary>
    Npx,

    /// <summary>ffmpeg.exe.</summary>
    Ffmpeg,

    /// <summary>ffprobe.exe.</summary>
    Ffprobe,

    /// <summary>Headless Chrome (render motorları için).</summary>
    ChromeHeadlessShell,

    /// <summary>uv (Python ortam yöneticisi).</summary>
    Uv,

    /// <summary>Gömülü Python yorumlayıcısı.</summary>
    Python,

    /// <summary>stable-diffusion.cpp sd / sd-server.</summary>
    StableDiffusionCpp,

    /// <summary>ComfyUI kurulumu.</summary>
    ComfyUi,

    /// <summary>nvidia-smi (GPU algılama).</summary>
    NvidiaSmi,
}
