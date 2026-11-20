namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Sonuçlar: alıcı ve okuyan veli sayısı ("X / Y veli okudu"), okumayan veliler (ad + çocuk adları) ve onay formunda öğrenci bazında onay/ret/cevapsız.</summary>
public sealed record AnnouncementResults(Guid Id, int RecipientCount, int ReadCount, IReadOnlyList<UnreadGuardianRow> Unread, ConsentSummary? Consent);
