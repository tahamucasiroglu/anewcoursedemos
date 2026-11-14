namespace TahaKonsept.Agent.Engines.Image.Catalog;

/// <summary>Bir model dosyasının stable-diffusion.cpp içindeki rolü; her rol bir CLI bayrağına karşılık gelir.</summary>
public enum ImageModelFileRole
{
    /// <summary>Tek başına difüzyon modeli (<c>--diffusion-model</c>).</summary>
    DiffusionModel,

    /// <summary>Tam checkpoint (<c>-m</c>); SD3.5 / SDXL gibi.</summary>
    FullModel,

    /// <summary>LLM metin kodlayıcı (<c>--llm</c>); Qwen3 vb.</summary>
    Llm,

    /// <summary>CLIP-L metin kodlayıcı (<c>--clip_l</c>).</summary>
    ClipL,

    /// <summary>CLIP-G metin kodlayıcı (<c>--clip_g</c>).</summary>
    ClipG,

    /// <summary>T5-XXL metin kodlayıcı (<c>--t5xxl</c>).</summary>
    T5xxl,

    /// <summary>VAE (<c>--vae</c>).</summary>
    Vae,
}
