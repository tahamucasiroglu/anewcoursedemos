namespace OkulYonetim.Domain.Communication;

/// <summary>Duyurunun kapsamı (spec 0011): okulun tamamı, bir şube, bir sınıf ya da tek öğrenci. Okul ve şube yalnızca kurucuda.</summary>
public enum AnnouncementScope
{
    School = 1,
    Branch = 2,
    Classroom = 3,
    Student = 4,
}
