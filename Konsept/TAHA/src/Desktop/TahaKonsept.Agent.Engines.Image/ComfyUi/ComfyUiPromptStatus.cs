namespace TahaKonsept.Agent.Engines.Image.ComfyUi;

/// <summary>ComfyUI <c>/history/{prompt_id}</c> kaydının özeti.</summary>
/// <param name="Completed">Çalışma bitti mi.</param>
/// <param name="Failed">Hata ile mi bitti.</param>
/// <param name="ErrorMessage">Hata mesajı (varsa).</param>
/// <param name="Images">Üretilen görseller.</param>
public sealed record ComfyUiPromptStatus(bool Completed, bool Failed, string? ErrorMessage, IReadOnlyList<ComfyUiImageRef> Images);
