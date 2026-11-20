namespace OkulYonetim.Application.Features.Students;

/// <summary>Yoklama listesindeki öğrenci (spec 0010): yalnızca kimlik ve ad-soyad (sağlık ve veli bilgisi yok).</summary>
public sealed record RosterStudent(Guid Id, string FullName);
