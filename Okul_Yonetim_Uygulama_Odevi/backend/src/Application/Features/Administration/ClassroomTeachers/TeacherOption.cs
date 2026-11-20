namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>Sınıf öğretmeni ekranındaki kişi (spec 0010): kimlik, ad-soyad ve e-posta (aynı adlı öğretmenleri ayırmak için).</summary>
public sealed record TeacherOption(Guid UserId, string FullName, string Email);
