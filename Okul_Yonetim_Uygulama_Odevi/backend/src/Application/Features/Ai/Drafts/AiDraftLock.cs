namespace OkulYonetim.Application.Features.Ai.Drafts;

/// <summary>
/// AI taslağı sırasının anahtarı (spec 0012 review L-2): "Vazgeç" (<c>AiDraftHandlers</c>) ve taslaklı gönderim
/// (Communication, <c>MessageHandlers</c>) aynı anahtarı alır — tek yerde, biri değişip öteki unutulursa gönder/vazgeç yarış
/// koruması sessizce kalkmasın diye.
/// </summary>
public static class AiDraftLock
{
    private const string KeyPrefix = "ai-draft:";

    public static string Key(Guid draftId) => KeyPrefix + draftId.ToString("N");
}
