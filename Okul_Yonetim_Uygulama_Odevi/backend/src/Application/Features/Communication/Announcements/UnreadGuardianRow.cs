namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuruyu henüz açmamış veli: ad-soyad ve duyurunun kapsadığı çocuklarının adları.</summary>
public sealed record UnreadGuardianRow(string GuardianName, IReadOnlyList<string> ChildNames);
