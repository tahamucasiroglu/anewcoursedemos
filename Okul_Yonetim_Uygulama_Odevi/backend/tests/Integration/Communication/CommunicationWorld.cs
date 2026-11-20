using OkulYonetim.Tests.Integration.Attendance;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 testlerinin hazır okulu: 0010'un hazır sınıfı (5-A + atanmış öğretmen) ve onda Ali (annesi ve babası veli) ile
/// Ayşe (annesi veli); aynı şubede başka öğretmenli 5-B ve onda Berk (annesi veli).
/// </summary>
public sealed record CommunicationWorld(
    AttendanceClass Class,
    Guid Ali,
    Guid Ayse,
    ParentAccount AliMother,
    ParentAccount AliFather,
    ParentAccount AyseMother,
    Guid OtherClassroomId,
    Guid OtherTeacherId,
    string OtherTeacher,
    Guid Berk,
    ParentAccount BerkMother);
