namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// "Onayla ve gönder" (spec 0014): öğretmenin onayladığı (düzenlenmiş) özet metni — boş olamaz, en fazla 1500 karakter — ve öğretmenin
/// panelde gördüğü taslak (kimlik + oluşturulma zamanı; review R1-2): gönderim o taslağa bağlıdır, değişmişse "Taslak değişti; paneli yenileyin.".
/// </summary>
public sealed record SendWeeklySummaryRequest(string? Text, Guid? DraftId, DateTimeOffset? DraftGeneratedAt);
