namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Video üretim hattının aşamaları (sırayla). Yer: Script/FactSheet/ScenePlan/SceneCode/Normalize → API; Images/Speech → sağlayıcıya göre API ya da ajan; Align/SceneRender/Compose/Mux → ajan.
/// </summary>
public enum PipelineStage
{
    /// <summary>Senaryo / konseptin incelenip senaryoya dönüştürülmesi (LLM).</summary>
    Script = 0,

    /// <summary>Gerçek bilgi özeti (tarih/spor vb. konularda uydurmayı önler).</summary>
    FactSheet = 1,

    /// <summary>Sahne planı (sahne süreleri, görsel ihtiyaçları).</summary>
    ScenePlan = 2,

    /// <summary>Sahne başına video motoru kodu (HyperFrames/Remotion/Revideo).</summary>
    SceneCode = 3,

    /// <summary>Sahne görselleri (yerel ya da online model).</summary>
    Images = 4,

    /// <summary>Türkçe okunuş: yazılış → okunuş (TTS'e giden metin).</summary>
    Normalize = 5,

    /// <summary>Seslendirme (TTS).</summary>
    Speech = 6,

    /// <summary>Ses ↔ metin hizalama (altyazı zamanlaması).</summary>
    Align = 7,

    /// <summary>Sahnelerin sessiz/altyazısız render edilmesi.</summary>
    SceneRender = 8,

    /// <summary>Birleştirme: altyazı (ASS karaoke) + ilerleme çubuğu (ffmpeg).</summary>
    Compose = 9,

    /// <summary>Ses + görüntü birleştirme, son dosya.</summary>
    Mux = 10,
}
