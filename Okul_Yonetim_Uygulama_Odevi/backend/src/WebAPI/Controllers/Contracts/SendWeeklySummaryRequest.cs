namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>"Onayla ve gönder" (spec 0014): öğretmenin onayladığı (düzenlenmiş) özet metni — boş olamaz, en fazla 1500 karakter.</summary>
public sealed record SendWeeklySummaryRequest(string? Text);
