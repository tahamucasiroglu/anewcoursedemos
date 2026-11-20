namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Yazılabilecek kişi: kimlik ve ad-soyad (e-posta ve telefon verilmez).</summary>
public sealed record ContactPerson(Guid UserId, string FullName);
