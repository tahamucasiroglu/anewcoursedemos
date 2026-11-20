namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Mesaj (spec 0011): metin (boş olamaz, en fazla 2000 karakter); spec 0012: sesli nottan gelen AI taslağının kimliği (varsa — gönderim taslağın onayıdır).</summary>
public sealed record SendMessageRequest(string? Body, Guid? AiDraftId = null);
