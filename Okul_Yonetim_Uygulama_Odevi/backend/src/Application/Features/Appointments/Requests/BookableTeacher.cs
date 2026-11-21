namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Velinin randevu alabileceği öğretmen (spec 0016 C-2): çocuğun sınıfının öğretmeni — kimlik ve ad-soyad.</summary>
public sealed record BookableTeacher(Guid UserId, string FullName);
