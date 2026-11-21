using System.Text.Json;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Tarife ekleme/düzenleme (spec 0015): ad ve tutar (JSON sayısı ya da metin, nokta ondalık — "85000.50").</summary>
public sealed record FeeScheduleRequest(string? Name, JsonElement? Amount);
