namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Bir günün değerleri (Europe/Istanbul günü).</summary>
/// <param name="Date">Gün (<c>yyyy-MM-dd</c>).</param>
/// <param name="Registrations">Yeni kayıt.</param>
/// <param name="Clips">Üretilen klip.</param>
/// <param name="ActiveUsers">Aktif kullanıcı (o gün en az bir aktivitesi olan).</param>
public sealed record AdminDayDto(string Date, int Registrations, int Clips, int ActiveUsers);
