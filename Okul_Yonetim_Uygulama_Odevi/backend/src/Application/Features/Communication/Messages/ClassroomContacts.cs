namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Öğretmenin sınıfı ve sınıftaki öğrenciler (velileriyle).</summary>
public sealed record ClassroomContacts(Guid ClassroomId, string ClassroomName, IReadOnlyList<StudentContacts> Students);
