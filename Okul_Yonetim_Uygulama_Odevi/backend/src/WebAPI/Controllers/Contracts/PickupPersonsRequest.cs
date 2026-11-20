using OkulYonetim.Application.Features.Students;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Teslim alabilecek kişilerin yeni listesi (en fazla 10; ad-soyad, yakınlık, telefon).</summary>
public sealed record PickupPersonsRequest(IReadOnlyList<PickupPersonInput>? Persons);
