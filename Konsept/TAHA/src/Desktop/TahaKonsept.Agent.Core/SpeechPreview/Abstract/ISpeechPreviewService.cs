using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.SpeechPreview.Abstract;

/// <summary>
/// Okunuş sözlüğündeki "Dinle" için kısa ses önizlemesi üretir: <c>Workspace/_previews/&lt;requestId&gt;.wav</c>;
/// web bu dosyayı ajanın yerel sunucusundan <c>/previews/&lt;requestId&gt;.wav?k=</c> ile çalar.
/// </summary>
public interface ISpeechPreviewService
{
    /// <summary>Önizleme üretir (hata durumunda da sonuç döner, istisna fırlatmaz).</summary>
    /// <param name="command">Komut.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<SpeechPreviewResult> CreatePreviewAsync(SpeechPreviewCommand command, CancellationToken ct);

    /// <summary>Önizleme dosyasının yerel yolunu çözer (yalnızca bu servisin ürettiği adlar).</summary>
    /// <param name="fileName">Dosya adı (ör. <c>{id}.wav</c>).</param>
    /// <returns>Var olan dosya ya da <c>null</c>.</returns>
    string? ResolvePreviewFile(string fileName);
}
