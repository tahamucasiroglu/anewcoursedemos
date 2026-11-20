namespace OkulYonetim.Application.Features.Students;

/// <summary>Teslim alabilecek kişi satırı (kurucu ve veli kartı): ad-soyad, yakınlık ve telefon (E.164).</summary>
public sealed record PickupPersonView(string FullName, string Relationship, string Phone);
