namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Aylık harcama (<c>GET api/ai/spend/month</c>, spec 0013): yuvarlanmamış USD toplamı ve ayın UTC başlangıcı.</summary>
/// <param name="TotalUsd">Toplam.</param>
/// <param name="MonthStartUtc">Ayın ilk anı (UTC).</param>
public sealed record MonthlySpendResponse(decimal TotalUsd, DateTimeOffset MonthStartUtc);
