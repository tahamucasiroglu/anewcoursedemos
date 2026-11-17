namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Yeniden üretimi başlatma isteği (spec 0020 Req 4–5).</summary>
/// <param name="Token">Yeniden üret bağlamı belirteci.</param>
/// <param name="BaseVersionId">Ayarları kullanılacak taban sürüm.</param>
/// <param name="Force">"Yine de baştan üret": konsept değişmese de üret.</param>
/// <param name="ConfirmOnline">Online çağrı uyarısı onaylandı.</param>
public sealed record RegenerateVersionRequest(string? Token, Guid BaseVersionId, bool Force = false, bool ConfirmOnline = false);
