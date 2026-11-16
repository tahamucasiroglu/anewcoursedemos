namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Üst bardaki aylık harcama (spec 0013 Req 15–16): kullanıcının içinde bulunulan İstanbul takvim ayındaki tüm kayıtlarının
/// yuvarlanmamış toplamı (USD, tüm profiller) ve ayın UTC başlangıcı. Gösterim biçimi Web'dedir.
/// </summary>
/// <param name="TotalUsd">Toplam (yuvarlanmamış).</param>
/// <param name="MonthStartUtc">Ayın ilk anı (UTC).</param>
public sealed record MonthlySpendDto(decimal TotalUsd, DateTimeOffset MonthStartUtc);
