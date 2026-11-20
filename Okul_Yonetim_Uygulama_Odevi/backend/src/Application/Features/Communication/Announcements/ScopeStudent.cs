namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru kapsamı için seçilebilen öğrenci: kimlik ve ad-soyad.</summary>
public sealed record ScopeStudent(Guid Id, string FullName);
