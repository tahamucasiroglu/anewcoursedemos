namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Öğrenci ve yazılabilecek (okulda etkin) velileri.</summary>
public sealed record StudentContacts(Guid StudentId, string StudentName, IReadOnlyList<ContactPerson> Guardians);
