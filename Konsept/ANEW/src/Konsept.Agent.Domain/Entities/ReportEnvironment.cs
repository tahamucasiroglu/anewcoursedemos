namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Raporun ortam bilgisi (spec 0022 Req 4–5): gönderilen işletim sistemi; makine adı ve kullanıcı profil klasörü yalnızca metinden
/// silmek için tutulur, rapora yazılmaz.
/// </summary>
/// <param name="OperatingSystem">İşletim sistemi (ör. <c>Microsoft Windows 10.0.26100</c>).</param>
/// <param name="MachineName">Makine adı (rapordan silinir).</param>
/// <param name="UserProfile">Kullanıcı profil klasörü (<c>%USERPROFILE%</c> olarak kısaltılır).</param>
public sealed record ReportEnvironment(string OperatingSystem, string? MachineName, string? UserProfile);
