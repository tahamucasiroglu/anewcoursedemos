using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Yoklamadan veliye giden tek çıkış (spec 0010 gereksinim 6–7, plan Risks 4): değişen öğrencilerin okulda etkin velilerine
/// uygulama içi bildirimi çağıranın işlemi içinde oluşturur ya da düzeltir ve gönderilecek e-postaları döndürür (kaydetmez,
/// kuyruğa bırakmaz). <c>AbsentStudentIds</c> velisi kayıtlı olmayan devamsızları saymak içindir. Uç yoktur; loglanmaz (T9).
/// </summary>
public sealed record NotifyAttendanceChangesCommand(DateOnly Day, IReadOnlyList<AttendanceChange> Changes, IReadOnlyCollection<Guid> AbsentStudentIds) : IRequest<AttendanceNoticeResult>;
