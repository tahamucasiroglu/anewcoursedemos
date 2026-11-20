namespace OkulYonetim.Application.Features.Students;

/// <summary>Teslim alabilecek kişi girişi: ad-soyad (≤ 100), yakınlık (≤ 50) ve telefon (spec 0005 kuralı) — üçü zorunlu.</summary>
public sealed record PickupPersonInput(string FullName, string Relationship, string Phone);
