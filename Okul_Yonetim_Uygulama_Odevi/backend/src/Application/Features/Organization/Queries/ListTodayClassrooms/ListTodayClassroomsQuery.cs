using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.ListTodayClassrooms;

/// <summary>
/// Bugünün okul günü bilgisi ve sınıfları (spec 0017 devamsızlık kartı); <c>BranchId</c> verilirse yalnızca o şube. Modüller arası okuma;
/// uç yok — yetki çağıranda (yönetici paneli kapısı). Sınıf filtresi şube görünürlüğünü miras alır.
/// </summary>
public sealed record ListTodayClassroomsQuery(Guid? BranchId) : IRequest<TodayClassrooms>;
