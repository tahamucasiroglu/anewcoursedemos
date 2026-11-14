namespace TahaKonsept.Agent.Engines.Image.ComfyUi;

/// <summary>ComfyUI'nin ürettiği bir görselin <c>/view</c> ile indirilebilmesi için gereken bilgiler.</summary>
/// <param name="FileName">Dosya adı.</param>
/// <param name="Subfolder">Alt klasör (boş olabilir).</param>
/// <param name="Type">Tür (<c>output</c>, <c>temp</c>, <c>input</c>).</param>
public sealed record ComfyUiImageRef(string FileName, string Subfolder, string Type);
