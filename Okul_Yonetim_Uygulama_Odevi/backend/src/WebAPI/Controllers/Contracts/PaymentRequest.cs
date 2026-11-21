using System.Text.Json;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Tahsilat kaydı (spec 0015): plan, tutar (JSON sayısı ya da metin), tarih (<c>yyyy-MM-dd</c>), yöntem (<c>Cash</c>/<c>BankTransfer</c>/<c>CardPos</c>) ve isteğe bağlı not.</summary>
public sealed record PaymentRequest(Guid PlanId, JsonElement? Amount, DateOnly? Date, string? Method, string? Note);
