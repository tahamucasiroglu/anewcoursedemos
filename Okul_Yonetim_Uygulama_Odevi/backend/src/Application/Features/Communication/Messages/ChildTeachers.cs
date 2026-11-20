namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Velinin çocuğu ve çocuğun sınıfında yazılabilecek öğretmenler.</summary>
public sealed record ChildTeachers(Guid StudentId, string StudentName, IReadOnlyList<ContactPerson> Teachers);
