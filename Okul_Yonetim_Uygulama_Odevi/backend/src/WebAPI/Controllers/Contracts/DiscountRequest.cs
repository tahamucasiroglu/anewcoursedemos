using System.Text.Json;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>İndirim/burs ekleme/düzenleme (spec 0015): ad ve yüzde (1–100 tam sayı; JSON sayısı ya da metin).</summary>
public sealed record DiscountRequest(string? Name, JsonElement? Percent);
